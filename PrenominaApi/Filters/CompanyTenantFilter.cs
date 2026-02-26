using Microsoft.AspNetCore.Mvc.Filters;
using PrenominaApi.Models.Dto.Input;

namespace PrenominaApi.Filters
{
    public class CompanyTenantFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var companyValue = httpContext.Items["companySelected"]?.ToString() ?? "";
            var tenantValue = httpContext.Items["tenantSelected"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(companyValue) && string.IsNullOrEmpty(tenantValue))
            {
                return;
            }

            // Validar y sanitizar company
            if (!TryParseCompany(companyValue, out decimal company))
            {
                return;
            }

            // Sanitizar tenant (remover caracteres peligrosos)
            var tenant = SanitizeTenant(tenantValue);

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is IHasCompanyTenant hasCompanyTenant)
                {
                    hasCompanyTenant.Company = company;
                    hasCompanyTenant.Tenant = tenant;
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No implementation needed after the action executes
        }

        /// <summary>
        /// Intenta parsear el valor de company de forma segura.
        /// </summary>
        private static bool TryParseCompany(string value, out decimal company)
        {
            company = 0;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Sanitizar entrada
            var sanitized = value.Trim();

            // Validar longitud máxima razonable
            if (sanitized.Length > 20)
                return false;

            // Intentar parsear
            if (!decimal.TryParse(sanitized, out company))
                return false;

            // Validar rango razonable
            if (company < -999 || company > 999999999)
                return false;

            return true;
        }

        /// <summary>
        /// Sanitiza el valor de tenant removiendo caracteres potencialmente peligrosos.
        /// </summary>
        private static string SanitizeTenant(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            // Trim y limitar longitud
            var sanitized = value.Trim();
            if (sanitized.Length > 100)
                sanitized = sanitized[..100];

            // Remover caracteres de control y potencialmente peligrosos
            sanitized = new string(sanitized
                .Where(c => !char.IsControl(c) && c != '\'' && c != '"' && c != ';' && c != '-' || c == '-')
                .ToArray());

            return sanitized;
        }
    }
}
