using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Middlewares
{
    public class UserMiddleware
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public readonly RequestDelegate _next;

        public UserMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                if (context.Request.Path == "/api/Auth")
                {
                    await _next(context);
                    return;
                }
                
                var userService = scope.ServiceProvider.GetRequiredService<IBaseServicePrenomina<User>>();
                if (context.User.Identity is not null && context.User.Identity.IsAuthenticated)
                {
                    var userId = context.User.FindFirst("UserId")?.Value;
                    var companySelected = context.Request.Headers["company"];
                    var tenantSelected = context.Request.Headers["tenant"];

                    if (!string.IsNullOrEmpty(userId))
                    {
                        var userDetails = userService.ExecuteProcess<string, UserDetails>(userId);

                        if (String.IsNullOrEmpty(companySelected) && userDetails?.role?.Code != RoleCode.Sudo)
                        {
                            throw new BadHttpRequestException("Seleccione una empresa");
                        }
                        
                        context.Items["companySelected"] = companySelected;
                        context.Items["tenantSelected"] = tenantSelected;
                        context.Items["UserDetails"] = userDetails;

                        var isPathMe = context.Request.Path == "/api/User/me";

                        if (userDetails.role?.Code != RoleCode.Sudo)
                        {
                            if (!userDetails.Companies.Exists(c => c.Id == Convert.ToDecimal(companySelected)) && !isPathMe)
                            {
                                throw new BadHttpRequestException("No tienes acceso a esta empresa");
                            }

                            var existInCenters = userDetails.Centers?.ToList().Exists(c => c.Id.Trim() == tenantSelected) ?? false;
                            var existInSupervisor = userDetails.Supervisors?.ToList().Exists(s => s.Id == Convert.ToDecimal(tenantSelected)) ?? false;

                            if (tenantSelected != "all" && !existInSupervisor && !existInCenters && !isPathMe)
                            {
                                throw new BadHttpRequestException("No tienes acceso a este centro o supervisor");
                            }
                        }
                    }
                }

                await _next(context);
            }
        }
    }
}
