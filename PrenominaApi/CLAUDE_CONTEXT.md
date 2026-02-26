# Contexto del Proyecto PrenominaApi

Este documento contiene el contexto completo del backend para acelerar futuras consultas con Claude.

---

## Resumen del Proyecto

**PrenominaApi** es el backend del sistema de gestión de nómina y asistencia.
- **Framework**: .NET 8.0 Web API
- **ORM**: Entity Framework Core
- **Base de datos**: SQL Server
- **Autenticación**: JWT

---

## Estructura del Proyecto

### Ubicación
```
/Users/jrivera/Develop/WebApps/PrenominaApi/PrenominaApi/
```

### Estructura de Carpetas
```
PrenominaApi/
├── Configuration/           # Configuración de servicios y middleware
├── Controllers/             # Controladores API REST
├── Data/
│   └── PrenominaDbContext.cs  # DbContext principal
├── Filters/
│   └── CompanyTenantValidationFilter.cs  # Validación de empresa/tenant
├── Middlewares/             # Middleware personalizado
├── Migrations/              # Scripts SQL de migración
├── Models/
│   ├── Dto/
│   │   ├── GlobalPropertyService.cs  # Propiedades globales por request
│   │   ├── Input/           # DTOs de entrada (requests)
│   │   └── Output/          # DTOs de salida (responses)
│   └── Prenomina/           # Entidades de Entity Framework
│       └── Enums/           # Enumeraciones
├── Repositories/
│   ├── IBaseRepository.cs
│   └── BaseRepository.cs    # Repositorio genérico
└── Services/
    └── Prenomina/           # Servicios de lógica de negocio
```

---

## Patrones y Convenciones

### Estructura de Controllers

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Filters;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ServiceFilter(typeof(CompanyTenantValidationFilter))]
    [ApiController]
    public class MyController : ControllerBase
    {
        private readonly MyService _service;
        private readonly GlobalPropertyService _globalPropertyService;

        public MyController(
            MyService service,
            GlobalPropertyService globalPropertyService)
        {
            _service = service;
            _globalPropertyService = globalPropertyService;
        }

        // Helper methods para obtener contexto
        private int GetCompanyId()
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "0";
            return int.Parse(company);
        }

        private string GetTenant()
        {
            return HttpContext.Items["tenantSelected"]?.ToString() ?? "";
        }
    }
}
```

### GlobalPropertyService

```csharp
// Propiedades disponibles
public class GlobalPropertyService
{
    public int YearOfOperation { get; set; }      // Año de operación
    public TypeTenant TypeTenant { get; set; }    // Tipo: Department o Supervisor
    public string? UserId { get; set; }           // GUID del usuario como string
}

// Uso en servicios
_globalPropertyService.YearOfOperation
_globalPropertyService.TypeTenant
_globalPropertyService.UserId  // Convertir a Guid: Guid.Parse(userId ?? Guid.Empty.ToString())
```

### Contexto HTTP (HttpContext.Items)

```csharp
// Estos valores son establecidos por el middleware/filtro
HttpContext.Items["companySelected"]   // ID de la empresa (string, parsear a int)
HttpContext.Items["tenantSelected"]    // ID del tenant/departamento (string)
```

---

## Tipos de Datos Importantes

### IDs
| Entidad | Tipo de ID | Notas |
|---------|-----------|-------|
| User | `Guid` | ID principal de usuario |
| Company | `int` | ID de empresa |
| Employee (Key) | `decimal` | Campo `Codigo` |
| Period | `int` | Auto-increment |

### Conversiones Comunes
```csharp
// UserId de string a Guid
Guid userId = Guid.Parse(_globalPropertyService.UserId ?? Guid.Empty.ToString());

// CompanyId de string a int
int companyId = int.Parse(HttpContext.Items["companySelected"]?.ToString() ?? "0");

// EmployeeCode de decimal a int (cuando se necesite)
int employeeCode = (int)key.Codigo;
```

---

## Entidades Principales

### User (Tabla: `[user]`)
```csharp
[Table("user")]
public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required Guid RoleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

### Key (Empleado)
```csharp
public class Key
{
    public decimal Codigo { get; set; }      // ID del empleado
    public int Company { get; set; }         // ID de empresa
    public int TypeNom { get; set; }         // Tipo de nómina
    public string? Center { get; set; }      // Centro/Departamento
    public decimal? Supervisor { get; set; } // ID del supervisor

    // Navigation properties
    public virtual Employee Employee { get; set; }
    public virtual CenterItem? CenterItem { get; set; }
    public virtual Tabulator Tabulator { get; set; }
}
```

### Period (Período de Nómina)
```csharp
public class Period
{
    public int NumPeriod { get; set; }
    public int TypePayroll { get; set; }
    public int Company { get; set; }
    public int Year { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly ClosingDate { get; set; }
    public DateOnly StartAdminDate { get; set; }
    public DateOnly ClosingAdminDate { get; set; }
    public DateOnly DatePayment { get; set; }
    public bool IsActive { get; set; }
}
```

### OvertimeAccumulation
```csharp
[Table("overtime_accumulations")]
public class OvertimeAccumulation
{
    [Key]
    public int Id { get; set; }
    public int EmployeeCode { get; set; }
    public int CompanyId { get; set; }
    public int AccumulatedMinutes { get; set; } = 0;
    public int UsedMinutes { get; set; } = 0;
    public int PaidMinutes { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### OvertimeMovementLog
```csharp
[Table("overtime_movement_logs")]
public class OvertimeMovementLog
{
    [Key]
    public int Id { get; set; }
    public int OvertimeAccumulationId { get; set; }
    public int EmployeeCode { get; set; }
    public int CompanyId { get; set; }
    public OvertimeMovementType MovementType { get; set; }
    public int Minutes { get; set; }
    public int BalanceAfter { get; set; }
    public DateOnly SourceDate { get; set; }
    public DateOnly? AppliedRestDate { get; set; }
    public TimeOnly? OriginalCheckIn { get; set; }
    public TimeOnly? OriginalCheckOut { get; set; }
    public string? Notes { get; set; }
    public Guid ByUserId { get; set; }           // GUID, no int
    public int? RelatedMovementId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual OvertimeAccumulation? OvertimeAccumulation { get; set; }
    public virtual User? User { get; set; }
    public virtual OvertimeMovementLog? RelatedMovement { get; set; }
}
```

---

## Enumeraciones

### TypeTenant
```csharp
public enum TypeTenant
{
    Department,   // Filtrar por departamento (Center)
    Supervisor    // Filtrar por supervisor
}
```

### OvertimeMovementType
```csharp
public enum OvertimeMovementType
{
    Accumulation = 1,      // Horas acumuladas
    UsedForRestDay = 2,    // Usadas para día de descanso
    DirectPayment = 3,     // Pago directo (sin acumular)
    ManualAdjustment = 4,  // Ajuste manual
    Cancellation = 5       // Cancelación de movimiento
}
```

---

## Servicios

### Patrón de Servicio
```csharp
public class MyService
{
    private readonly PrenominaDbContext _context;
    private readonly IBaseRepository<Key> _keyRepository;
    private readonly GlobalPropertyService _globalPropertyService;

    public MyService(
        PrenominaDbContext context,
        IBaseRepository<Key> keyRepository,
        GlobalPropertyService globalPropertyService)
    {
        _context = context;
        _keyRepository = keyRepository;
        _globalPropertyService = globalPropertyService;
    }

    // Métodos asíncronos
    public async Task<ResultDto> DoSomethingAsync(InputDto input, int companyId, string? userId)
    {
        // Lógica de negocio
    }
}
```

### Consultas con EF Core
```csharp
// Obtener empleados con filtros
var employees = await _keyRepository.GetContextEntity().AsNoTracking()
    .Where(k =>
        k.Company == companyId &&
        k.TypeNom == typeNomina &&
        (tenant == "-999" ||
            (_globalPropertyService.TypeTenant == TypeTenant.Department ?
                k.Center == tenant :
                k.Supervisor == Convert.ToDecimal(tenant)))
    )
    .Select(k => new {
        k.Codigo,
        FullName = $"{k.Employee.Name} {k.Employee.LastName} {k.Employee.MLastName}",
        Department = k.CenterItem != null ? k.CenterItem.DepartmentName : string.Empty
    })
    .ToListAsync();
```

---

## DTOs

### Ubicación
- **Input**: `/Models/Dto/Input/`
- **Output**: `/Models/Dto/Output/`

### Convención de Nombres
```
{Accion}{Entidad}Input.cs    // Ej: AccumulateOvertimeInput.cs
{Entidad}{Tipo}Output.cs     // Ej: OvertimeSummaryOutput.cs
```

### Ejemplo
```csharp
// Input
public class AccumulateOvertimeInput
{
    public int EmployeeCode { get; set; }
    public string SourceDate { get; set; }
    public int Minutes { get; set; }
    public string? CheckIn { get; set; }
    public string? CheckOut { get; set; }
    public string? Notes { get; set; }
}

// Output
public class OvertimeOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int? MovementId { get; set; }
    public int NewBalance { get; set; }
    public string NewBalanceFormatted { get; set; }
}
```

---

## Migraciones SQL

### Ubicación
```
/Migrations/
```

### Convención de Nombres
```
Add{Feature}.sql
Update{Table}.sql
```

### Ejemplo de Migración
```sql
-- Verificar si existe antes de crear
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'my_table')
BEGIN
    CREATE TABLE my_table (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        -- columnas...
    );
    PRINT 'Created table: my_table';
END
GO

-- Crear índices
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MyTable_Column')
BEGIN
    CREATE NONCLUSTERED INDEX IX_MyTable_Column
    ON my_table (column_name);
END
GO
```

---

## DbContext

### Ubicación
```
/Data/PrenominaDbContext.cs
```

### Registro de Entidades
```csharp
public class PrenominaDbContext : DbContext
{
    public DbSet<OvertimeAccumulation> overtimeAccumulations { get; set; }
    public DbSet<OvertimeMovementLog> overtimeMovementLogs { get; set; }
    // ... otras entidades
}
```

---

## Notas Importantes

1. **Una clase por archivo**: Cada archivo `.cs` debe contener **UNA SOLA clase, enum o interface**. No agrupar múltiples tipos en el mismo archivo.
2. **Tabla de usuarios**: Se llama `[user]` (minúscula, usar corchetes por palabra reservada)
3. **User.Id es GUID**, no int
4. **CompanyId y Tenant** vienen de `HttpContext.Items`, no de `GlobalPropertyService`
5. **GlobalPropertyService.UserId** es `string?`, convertir a `Guid` cuando se necesite
6. **Key.Codigo** es `decimal`, convertir a `int` cuando se trabaje con otras tablas
7. **Tenant "-999"** significa "todos los departamentos/supervisores"
8. **Usar AsNoTracking()** para consultas de solo lectura
9. **ModelClock.dll** requiere .NET Framework 4.8 (solo compila en Windows)

### Estructura de Archivos - Una Clase Por Archivo
```
// CORRECTO - cada tipo en su propio archivo
Models/
├── Dto/
│   ├── Input/
│   │   ├── AccumulateOvertimeInput.cs      -> class AccumulateOvertimeInput
│   │   └── PayOvertimeDirectInput.cs       -> class PayOvertimeDirectInput
│   └── Output/
│       ├── OvertimeSummaryOutput.cs        -> class OvertimeSummaryOutput
│       └── OvertimeOperationResult.cs      -> class OvertimeOperationResult
└── Prenomina/
    ├── OvertimeAccumulation.cs             -> class OvertimeAccumulation
    ├── OvertimeMovementLog.cs              -> class OvertimeMovementLog
    └── Enums/
        └── OvertimeMovementType.cs         -> enum OvertimeMovementType

// INCORRECTO - NO agrupar múltiples tipos
Models/Dto/Output/OvertimeOutputs.cs        -> múltiples clases (NO HACER)
```

---

## Endpoints API

### Autenticación
```
POST /api/auth/login
POST /api/auth/refresh
```

### Asistencia
```
GET  /api/attendance
GET  /api/attendance/init
GET  /api/attendance/download
POST /api/attendance/apply-incident
POST /api/attendance/change
```

### Períodos
```
GET  /api/period
POST /api/period/create
POST /api/period/status
```

### Horas Extra
```
GET  /api/overtime-accumulation/summary
GET  /api/overtime-accumulation/balance/{employeeCode}
GET  /api/overtime-accumulation/movements
POST /api/overtime-accumulation/accumulate
POST /api/overtime-accumulation/pay-direct
POST /api/overtime-accumulation/use-for-rest-day
POST /api/overtime-accumulation/adjust
POST /api/overtime-accumulation/cancel
POST /api/overtime-accumulation/process-batch
```

---

## Comandos

```bash
# Build (puede fallar en macOS por ModelClock)
dotnet build

# Run
dotnet run

# Migrations EF Core
dotnet ef migrations add MigrationName
dotnet ef database update
```

---

## Dependencias Clave

- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.Data.SqlClient
- Microsoft.AspNetCore.Authentication.JwtBearer
- System.Text.Json
