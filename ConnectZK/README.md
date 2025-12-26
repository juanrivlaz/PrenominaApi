# ConnectZK - Guía de Uso Eficiente

## Optimizaciones Implementadas

### 1. Pool de Conexiones
La clase `ConnectZK` ahora implementa un patrón Factory con pool de conexiones para reutilizar instancias y reducir la sobrecarga de crear objetos COM repetidamente.

```csharp
// ? CORRECTO - Usar Factory Pattern
var zk = ConnectZK.GetInstance("192.168.1.100", 4370);
var users = zk.GetUsers();

// ? INCORRECTO - No crear instancias directamente
var zk = new ConnectZK("192.168.1.100", 4370); // Constructor ahora es privado
```

### 2. Filtrado Inteligente de Logs
Los logs del SDK zkemkeeper son automáticamente filtrados **sin afectar la salida JSON**.

**Cómo funciona:**
- `FilteredConsoleWriter` intercepta todas las escrituras a la consola
- Filtra líneas que contengan patrones del SDK zkemkeeper
- **Preserva salida JSON** (cualquier línea que comience con `{` o `[`)
- Solo bloquea logs innecesarios del SDK nativo

**Patrones de logs filtrados:**
```
- "zkemkeeper"
- "ZKEM"
- "Connect_Net"
- "Disconnect"
- "ReadAllUserID"
- "ReadGeneralLogData"
- "SSR_GetAllUserInfo"
- "GetUserTmpExStr"
- "[ZK]"
- "COM:"
- "Interop"
```

**Implementación en ZKBridgeApp:**
```csharp
var originalOut = Console.Out;
Console.SetOut(new ConnectZK.FilteredConsoleWriter(originalOut));
```

### 3. Manejo de Recursos con IDisposable
Aunque el pool mantiene las conexiones, puedes liberar recursos manualmente si es necesario:

```csharp
// Limpieza manual de conexiones inactivas (por defecto 5 minutos)
ConnectZK.CleanupIdleConnections(idleMinutes: 5);
```

### 4. Gestión Automática de Conexiones
- Las conexiones se abren solo cuando son necesarias
- Se cierran automáticamente después de cada operación
- Se reutilizan las instancias IZKEM para reducir overhead

## Mejoras de Rendimiento

### Antes:
```
- Nueva instancia COM por cada operación
- Logs excesivos del SDK contaminando stdout
- Sin reutilización de conexiones
- Process.Start() capturaba logs mezclados con JSON
```

### Después:
```
? Pool de conexiones reutilizables
? Logs del SDK filtrados inteligentemente
? JSON siempre visible en stdout
? Gestión automática de recursos
? Instancia única por IP:Port
```

## Uso en ClockService

### ? Continuar usando ZKBridgeApp.exe (Recomendado)
Tu código actual **NO necesita cambios**. El filtrado funciona automáticamente:

```csharp
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = @"tools/zkbridge/ZKBridgeApp.exe",
        Arguments = $"{clock.Ip} {clock.Port} getusers",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    }
};

process.Start();
string output = await process.StandardOutput.ReadToEndAsync(); // ? Ahora contiene solo JSON limpio
string error = await process.StandardError.ReadToEndAsync();
```

**Beneficios inmediatos:**
- ? `output` contiene solo JSON válido
- ? Logs del SDK zkemkeeper filtrados
- ? Sin cambios en ClockService
- ? Sin afectar deserialización JSON

### Alternativa: Uso directo (Mayor rendimiento, menos aislamiento)
Si decides migrar a uso directo de ConnectZK en lugar de Process.Start():

```csharp
public async Task<IEnumerable<ClockUser>> ExecuteProcess(GetClockUser getClockUser)
{
    var clock = _repository.GetById(getClockUser.Id);
    if (clock == null)
    {
        throw new BadHttpRequestException("El reloj no existe.");
    }

    try
    {
        var zk = ConnectZK.ConnectZK.GetInstance(clock.Ip, clock.Port ?? 4370);
        var users = await Task.Run(() => zk.GetUsers());
        
        return users.Select(u => new ClockUser
        {
            EnrollNumber = u.EnrollNumber,
            Name = u.Name,
            Password = u.Password,
            Privilege = u.Privilege,
            Enabled = u.Enabled
        });
    }
    catch (Exception ex)
    {
        Log.Error($"Error comunicando con reloj {clock.Label}: {ex.Message}");
        throw new Exception($"Ocurrió un error en la comunicación con el reloj");
    }
}
```

## Limpieza Periódica (Opcional)

Puedes agregar una tarea en segundo plano para limpiar conexiones inactivas:

```csharp
// En AttendanceJob o similar
private async void CleanupConnections(object? state)
{
    ConnectZK.ConnectZK.CleanupIdleConnections(idleMinutes: 10);
}
```

## Comandos ZKBridgeApp

```bash
# Obtener usuarios básicos
ZKBridgeApp.exe 192.168.1.100 4370 getusers

# Obtener usuarios completos con huellas
ZKBridgeApp.exe 192.168.1.100 4370 getfullusers

# Obtener checadas
ZKBridgeApp.exe 192.168.1.100 4370 getcheckins

# Limpiar checadas del dispositivo
ZKBridgeApp.exe 192.168.1.100 4370 clearcheckins

# Limpiar pool de conexiones inactivas
ZKBridgeApp.exe 192.168.1.100 4370 cleanup
```

## Troubleshooting

### ? Si el JSON sigue llegando vacío:
1. ? Verificar que `FilteredConsoleWriter` esté siendo usado en `ZKBridgeApp`
2. ? Agregar `Console.Out.Flush()` antes de terminar el proceso
3. ? Verificar que el JSON comience con `{` o `[` (se preserva automáticamente)

### ? Si los logs siguen apareciendo:
1. Agregar más patrones a `_zkLogPatterns` en `FilteredConsoleWriter`
2. Verificar que la inicialización del filtro sea al inicio de `Main()`
3. Revisar si hay logs en `stderr` en lugar de `stdout`

### ? Si hay problemas de conexión:
1. Verificar que el pool no tenga conexiones corruptas
2. Llamar a `CleanupIdleConnections()` para forzar limpieza
3. Verificar firewall y conectividad de red

### ? Problemas de rendimiento:
1. Reducir el tiempo de idle en `CleanupIdleConnections()`
2. Considerar migrar de Process.Start() a uso directo
3. Monitorear con Serilog las operaciones lentas

## Migración de Código Existente

? **NO requiere cambios en `ClockService.cs`**

Tu código actual continúa funcionando exactamente igual:

```csharp
// Este código NO necesita cambios
var process = new Process { ... };
process.Start();
string output = await process.StandardOutput.ReadToEndAsync();
var users = JsonSerializer.Deserialize<List<User>>(output); // ? Funciona perfectamente
```

Los beneficios se obtienen automáticamente:

? JSON limpio sin logs del SDK  
? Mejor gestión de memoria  
? Reutilización de conexiones  
? Recursos liberados correctamente  
? Sin cambios en tu código existente  

## Arquitectura de Filtrado

```
???????????????????????????????????????
?      ZKBridgeApp.exe (Main)         ?
?  Console.SetOut(FilteredWriter)     ?
???????????????????????????????????????
              ?
              ?
???????????????????????????????????????
?     FilteredConsoleWriter           ?
?  • Intercepta Write/WriteLine       ?
?  • Detecta patrones de logs ZK      ?
?  • Preserva JSON ({, [)             ?
???????????????????????????????????????
              ?
              ?
???????????????????????????????????????
?       ConnectZK Library             ?
?  • Pool de conexiones               ?
?  • Gestión de recursos COM          ?
?  • Operaciones zkemkeeper           ?
???????????????????????????????????????
              ?
              ?
???????????????????????????????????????
?    Process.StandardOutput           ?
?  ? Solo JSON válido                ?
?  ? Sin logs del SDK                ?
???????????????????????????????????????
```

## Ejemplo de Salida

### ? Antes (con logs del SDK):
```
[zkemkeeper] Initializing connection...
Connect_Net: Attempting to connect to 192.168.1.100:4370
[ZKEM] ReadAllUserID: Starting...
{"EnrollNumber":"1","Name":"Juan"}
[ZKEM] SSR_GetAllUserInfo: Processing...
{"EnrollNumber":"2","Name":"Maria"}
Disconnect: Connection closed.
```

### ? Después (solo JSON):
```json
[{"EnrollNumber":"1","Name":"Juan"},{"EnrollNumber":"2","Name":"Maria"}]
```

Perfecto para `Process.StandardOutput.ReadToEndAsync()` y deserialización directa. ??
