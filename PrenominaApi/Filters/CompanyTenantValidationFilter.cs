using Microsoft.AspNetCore.Mvc.Filters;

namespace PrenominaApi.Filters
{
    public class CompanyTenantValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var company = httpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = httpContext.Items["tenantSelected"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(company) || string.IsNullOrEmpty(tenant))
            {
                context.Result = new Microsoft.AspNetCore.Mvc.BadRequestObjectResult("Company or Tenant information is missing.");
            }
        }
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No implementation needed after the action executes
        }
    }
}