using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PrenominaApi.Filters
{
    /// <summary>
    /// Filtro que valida que los headers company y tenant estén presentes
    /// y tengan valores válidos antes de procesar la solicitud.
    /// </summary>
    public class CompanyTenantValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var companyValue = httpContext.Items["companySelected"]?.ToString() ?? "";
            var tenantValue = httpContext.Items["tenantSelected"]?.ToString() ?? "";

            // Validar presencia
            if (string.IsNullOrWhiteSpace(companyValue))
            {
                context.Result = new BadRequestObjectResult(new
                {
                    Message = "Es necesario seleccionar una empresa.",
                    Field = "company"
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(tenantValue))
            {
                context.Result = new BadRequestObjectResult(new
                {
                    Message = "Es necesario seleccionar un departamento o supervisor.",
                    Field = "tenant"
                });
                return;
            }

            // Validar formato de company
            if (!int.TryParse(companyValue, out int companyId) || companyId <= 0)
            {
                // Permitir valor especial -999 para superusuarios
                if (companyValue != "-999")
                {
                    context.Result = new BadRequestObjectResult(new
                    {
                        Message = "El identificador de empresa no es válido.",
                        Field = "company"
                    });
                    return;
                }
            }

            // Validar longitud de tenant (prevenir ataques de overflow)
            if (tenantValue.Length > 100)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    Message = "El identificador de departamento/supervisor no es válido.",
                    Field = "tenant"
                });
                return;
            }

            // Validar caracteres peligrosos en tenant
            if (ContainsDangerousCharacters(tenantValue))
            {
                context.Result = new BadRequestObjectResult(new
                {
                    Message = "El identificador de departamento/supervisor contiene caracteres no permitidos.",
                    Field = "tenant"
                });
                return;
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No implementation needed after the action executes
        }

        /// <summary>
        /// Verifica si el valor contiene caracteres potencialmente peligrosos
        /// para prevenir inyección SQL y otros ataques.
        /// </summary>
        private static bool ContainsDangerousCharacters(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Lista de patrones peligrosos comunes para SQL injection
            var dangerousPatterns = new[]
            {
                "'", "\"", ";", "--", "/*", "*/", "xp_", "sp_",
                "exec", "execute", "insert", "update", "delete",
                "drop", "alter", "create", "truncate", "union",
                "<script", "javascript:", "onerror", "onload"
            };

            var lowerValue = value.ToLowerInvariant();

            return dangerousPatterns.Any(pattern =>
                lowerValue.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
    }
}
