using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina;
using Newtonsoft.Json;

namespace PrenominaApi.Middlewares
{
    public class SetGlobalPropertyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GlobalPropertyService _globalPropertyService;
        private readonly IServiceScopeFactory _scopeFactory;

        public SetGlobalPropertyMiddleware(
            RequestDelegate next,
            GlobalPropertyService globalPropertyService,
            IServiceScopeFactory scopeFactory
        ) {
            _next = next;
            _globalPropertyService = globalPropertyService;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var sysConfigService = scope.ServiceProvider.GetRequiredService<IBaseServicePrenomina<SystemConfig>>();
                _globalPropertyService.YearOfOperation = sysConfigService.ExecuteProcess<string, SysYearOperation>("Year-Operation").Year;
                _globalPropertyService.TypeTenant = TypeTenant.Department;

                //get system type tenant
                var findSysTypeTenant = sysConfigService.GetById("Type-Tenant");
                if (findSysTypeTenant != null)
                {
                    var value = JsonConvert.DeserializeObject<SysTypeTenant>(findSysTypeTenant.Data);
                    _globalPropertyService.TypeTenant = value?.TypeTenant ?? _globalPropertyService.TypeTenant;
                }

                _globalPropertyService.UserId = context.User.FindFirst("UserId")?.Value;

                await _next(context);
            }
        }
    }
}
