# Proyecto PrenominaApi - Backend .NET

## Stack
- .NET 8.0 Web API + Entity Framework Core
- Database: SQL Server
- Frontend: Angular en `/Users/jrivera/Develop/WebApps/Apsi/NewVersion/front/Prenomina/`

## Reglas OBLIGATORIAS

### Una Clase Por Archivo
Cada archivo `.cs` debe contener **una sola clase**. No agrupar múltiples clases, enums o interfaces en el mismo archivo.
```
// CORRECTO
Models/Dto/Output/OvertimeSummaryOutput.cs    -> class OvertimeSummaryOutput
Models/Dto/Output/OvertimeOperationResult.cs  -> class OvertimeOperationResult

// INCORRECTO - NO HACER
Models/Dto/Output/OvertimeOutputs.cs -> múltiples clases
```

### Obtener Contexto en Controllers
```csharp
// CompanyId y Tenant desde HttpContext.Items (NO de GlobalPropertyService)
private int GetCompanyId() => int.Parse(HttpContext.Items["companySelected"]?.ToString() ?? "0");
private string GetTenant() => HttpContext.Items["tenantSelected"]?.ToString() ?? "";

// UserId desde GlobalPropertyService (es string?, convertir a Guid)
Guid.Parse(_globalPropertyService.UserId ?? Guid.Empty.ToString())
```

### GlobalPropertyService - Propiedades Disponibles
```csharp
_globalPropertyService.YearOfOperation  // int
_globalPropertyService.TypeTenant       // enum (Department/Supervisor)
_globalPropertyService.UserId           // string? (GUID como string)
// CompanyId y Tenant NO están aquí, usar HttpContext.Items
```

### Tipos de IDs
| Entidad | Tipo |
|---------|------|
| User.Id | `Guid` |
| CompanyId | `int` |
| Key.Codigo | `decimal` |

### Tabla de Usuarios
```sql
[user]  -- minúscula, con corchetes (palabra reservada)
```

### Estructura de Controller
```csharp
[Route("api/[controller]"), Authorize]
[ServiceFilter(typeof(CompanyTenantValidationFilter))]
[ApiController]
public class MyController : ControllerBase
```

## Notas
- Tenant "-999" = todos los departamentos
- Usar AsNoTracking() para consultas de solo lectura
- ModelClock.dll requiere .NET Framework 4.8 (solo Windows)

## Contexto Detallado
Ver @CLAUDE_CONTEXT.md para documentación completa.
