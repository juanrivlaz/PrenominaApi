using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Models.Dto.Input;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IgnoreIncidentToTenantController : ControllerBase
    {
        private readonly IBaseServicePrenomina<IgnoreIncidentToTenant> _service;
        private readonly GlobalPropertyService _globalProperty;

        public IgnoreIncidentToTenantController(IBaseServicePrenomina<IgnoreIncidentToTenant> service,  GlobalPropertyService globalProperty)
        {
            _service = service;
            _globalProperty = globalProperty;
        }

        [HttpGet("get-tenants")]
        public ActionResult<TenantsForIgnoreIncident> GetTenants()
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            var result = _service.ExecuteProcess<int, TenantsForIgnoreIncident>(companyId);

            return Ok(result);
        }

        [HttpGet("{tenantId}")]
        public ActionResult<IEnumerable<IgnoreIncidentToTenant>> GetByTenantId(string tenantId)
        {
            var typeTenant = _globalProperty.TypeTenant;

            var result = _service.GetByFilter((item) => typeTenant == TypeTenant.Department ? item.DepartmentCode?.TrimEnd() == tenantId : item.SupervisorId == int.Parse(tenantId));

            return Ok(result);
        }

        [HttpPost]
        public ActionResult<bool> AddIgnoreIncident([FromBody] AddIgnoreIncidentToTenant addIgnoreIncidentToTenant)
        {
            var result = _service.ExecuteProcess<AddIgnoreIncidentToTenant, bool>(addIgnoreIncidentToTenant);

            return Ok(result);
        }
    }
}
