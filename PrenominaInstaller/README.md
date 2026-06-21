# Instalador Prenomina (API + Frontend)

Aplicación de escritorio (WPF) que instala el **Prenomina API** y el **frontend Angular** como
**Servicios de Windows** (hosting Kestrel autónomo, **sin IIS, sin hosting bundle, sin Node**).
El usuario solo captura los valores de configuración; el instalador hace todo lo demás.

## Qué hace al presionar "Instalar"

**API** (servicio `PrenominaApi`):
1. Copia el API empaquetado a la ruta de instalación (por defecto `C:\PrenominaApi`).
2. Escribe `appsettings.Production.json` con clave JWT, puerto, CORS y zona horaria.
3. Crea las variables de entorno de máquina con las credenciales de la BD:
   `SERVER_DB`, `NAME_APSI_DB`, `NAME_PRENOMINA_DB`, `USER_DB`, `PASSWORD_DB`, `JWT_SECRET_KEY`.
4. Crea el Servicio de Windows (`sc create`, arranque automático) con reinicio ante fallos.
5. Abre el puerto en el Firewall e inicia el servicio (verifica `/swagger`).

**Frontend** (servicio `PrenominaWeb`, opcional — casilla en el formulario):
6. Copia el servidor estático `PrenominaWeb` + el build de Angular a `C:\PrenominaWeb\wwwroot`.
7. Escribe `wwwroot\runtime-config.json` con la **URL del API** capturada
   (el front la lee en caliente vía `window.env`, **sin recompilar**).
8. Crea el servicio en el puerto web, abre el firewall e inicia (verifica la página).

> El usuario **no** instala IIS, ni hosting bundle, ni Node, ni edita scripts. Solo llena el formulario.

## Requisitos

- **Para compilar el instalador**: Windows + SDK de .NET 8 (WPF solo compila en Windows) + Node.js
  (solo para `ng build` del frontend).
- **En el servidor destino**: nada. Tanto el API como el servidor del frontend se publican
  *self-contained* (incluyen el runtime de .NET).

## Cómo generar el instalador

Desde Windows, en esta carpeta:

```powershell
# Ajusta -FrontendDir a la ruta real del proyecto Angular en Windows
powershell -ExecutionPolicy Bypass -File .\build-installer.ps1 -FrontendDir C:\ruta\al\front
```

El script:
- Publica el API self-contained          -> `api\`
- Publica el servidor estático del front  -> `web\`
- Compila el frontend (`ng build`)        -> `webroot\`
- Publica el instalador como `.exe` único -> `dist\`

Para omitir el frontend: `.\build-installer.ps1 -SkipWeb`

Resultado: copia la carpeta `dist\` al servidor y ejecuta **`PrenominaInstaller.exe` como administrador**.

> El instalador requiere privilegios de administrador (definido en `app.manifest`); Windows
> pedirá elevación (UAC) automáticamente. La carpeta `dist\` incluye el `.exe` más las carpetas
> `api`, `web` y `webroot` con los payloads.

### Pasos manuales equivalentes (si no usas el script)

```powershell
# 1) API -> api\
dotnet publish ..\PrenominaApi\PrenominaApi.csproj -c Release -r win-x64 --self-contained true -o .\api
# 2) Servidor estático del front -> web\
dotnet publish ..\PrenominaWeb\PrenominaWeb.csproj -c Release -r win-x64 --self-contained true -o .\web
# 3) Build Angular -> webroot\
cd C:\ruta\al\front; npm ci; npm run build
Copy-Item .\dist\prenomina\browser <ruta>\PrenominaInstaller\webroot -Recurse -Force
# 4) Instalador -> dist\
dotnet publish .\PrenominaInstaller.csproj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -o .\dist
```

## Estructura

```
PrenominaInstaller/
├── PrenominaInstaller.csproj
├── app.manifest                 # requireAdministrator
├── App.xaml / App.xaml.cs       # estilos / arranque
├── MainWindow.xaml / .cs        # formulario + log de progreso
├── Models/InstallConfig.cs      # valores capturados + validación
├── Services/InstallerService.cs # toda la lógica de instalación (API + front)
├── build-installer.ps1          # genera todos los payloads + instalador
├── api/                         # payload: publish del API — no se versiona
├── web/                         # payload: servidor estático PrenominaWeb — no se versiona
└── webroot/                     # payload: build de Angular — no se versiona

../PrenominaWeb/                 # servidor estático mínimo (Kestrel) para el frontend
```

## Notas

- El **frontend es opcional**: la casilla "Instalar también el frontend" lo activa/desactiva.
- La **URL del API** capturada se escribe en `wwwroot\runtime-config.json`; el front la lee en
  caliente (`main.ts` → `window.env` → `SecureConfigService`). Para cambiarla luego, basta editar
  ese archivo y reiniciar el servicio `PrenominaWeb` (no requiere recompilar Angular).
- Usa el nombre de máquina o IP del servidor en la URL del API (lo verá el navegador del cliente),
  no `localhost`, salvo que solo se use desde el propio servidor.
- La clave JWT debe tener al menos 32 caracteres. Usa el botón **Generar** para crear una segura.
- Para **actualizar** una instalación, vuelve a ejecutar el instalador: detiene y reemplaza los
  servicios y archivos automáticamente.
- Logs en ejecución: carpeta `logs\` dentro de cada ruta de instalación.
- El cambio que habilita el modo servicio está en `PrenominaApi/Program.cs` y `PrenominaWeb/Program.cs`
  (`builder.Host.UseWindowsService()`), con el paquete `Microsoft.Extensions.Hosting.WindowsServices`.
```
