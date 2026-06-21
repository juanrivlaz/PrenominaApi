using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Output;
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

                // Centro/supervisor seleccionado (header "tenant"); "-999" = TODOS.
                var tenantHeader = context.Request.Headers["tenant"].FirstOrDefault();
                _globalPropertyService.Tenant = string.IsNullOrWhiteSpace(tenantHeader) ? "-999" : tenantHeader;

                // Alcance del usuario: para no-sudo, "TODOS" se limita a los centros/supervisores
                // asignados (de la empresa activa). UserMiddleware (ejecutado antes) deja los
                // detalles en context.Items.
                var userDetails = context.Items["UserDetails"] as UserDetails;
                _globalPropertyService.IsSudo = userDetails?.role?.Code == RoleCode.Sudo;

                decimal? activeCompany = decimal.TryParse(context.Request.Headers["company"].FirstOrDefault(), out var comp) ? comp : null;

                _globalPropertyService.AssignedCenterIds = userDetails?.Centers?
                    .Where(c => !string.IsNullOrWhiteSpace(c.Id) && (activeCompany == null || c.Company == activeCompany))
                    .Select(c => c.Id.Trim())
                    .ToList() ?? new List<string>();
                _globalPropertyService.AssignedSupervisorIds = userDetails?.Supervisors?
                    .Where(s => activeCompany == null || s.Company == activeCompany)
                    .Select(s => s.Id)
                    .ToList() ?? new List<decimal>();

                await _next(context);
            }
        }
    }
}
