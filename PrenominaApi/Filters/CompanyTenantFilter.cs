using Microsoft.AspNetCore.Mvc.Filters;
using PrenominaApi.Models.Dto.Input;

namespace PrenominaApi.Filters
{
    public class CompanyTenantFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var company = httpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = httpContext.Items["tenantSelected"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(company) && string.IsNullOrEmpty(tenant))
            {
                return;
            }

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument is IHasCompanyTenant hasCompanyTenant)
                {
                    hasCompanyTenant.Company = Convert.ToDecimal(company);
                    hasCompanyTenant.Tenant = tenant;
                }
            }
        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No implementation needed after the action executes
        }
    }
}
