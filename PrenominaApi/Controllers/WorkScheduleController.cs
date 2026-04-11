using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Filters;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [ServiceFilter(typeof(CompanyTenantValidationFilter))]
    public class WorkScheduleController : ControllerBase
    {
        private readonly WorkScheduleService _service;

        public WorkScheduleController(WorkScheduleService service)
        {
            _service = service;
        }

        private int GetCompanyId()
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "0";
            return int.Parse(company);
        }

        [HttpGet]
        public ActionResult<List<WorkScheduleOutput>> List()
        {
            return Ok(_service.List(GetCompanyId()));
        }

        [HttpGet("{id}")]
        public ActionResult<WorkScheduleOutput> GetById(Guid id)
        {
            var result = _service.GetById(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public ActionResult<WorkScheduleOutput> Create([FromBody] WorkScheduleInput input)
        {
            return Ok(_service.Create(input, GetCompanyId()));
        }

        [HttpPut("{id}")]
        public ActionResult<bool> Update(Guid id, [FromBody] WorkScheduleInput input)
        {
            var ok = _service.Update(id, input);
            if (!ok) return NotFound();
            return Ok(true);
        }

        [HttpDelete("{id}")]
        public ActionResult<bool> Delete(Guid id)
        {
            var ok = _service.Delete(id);
            if (!ok) return NotFound();
            return Ok(true);
        }

        [HttpGet("{id}/employees")]
        public ActionResult<List<int>> GetAssignedEmployees(Guid id)
        {
            return Ok(_service.GetEmployeesAssignedToSchedule(id, GetCompanyId()));
        }

        [HttpPost("assign")]
        public ActionResult<bool> AssignBatch([FromBody] AssignWorkScheduleInput input)
        {
            return Ok(_service.AssignBatchEmployeeSchedule(
                input.EmployeeCodes,
                GetCompanyId(),
                input.WorkScheduleId,
                input.EffectiveFrom));
        }

        [HttpGet("active-assignments")]
        public ActionResult GetActiveAssignments()
        {
            return Ok(_service.GetActiveEmployeeAssignments(GetCompanyId()));
        }

        [HttpGet("activity-configs")]
        public ActionResult GetActivityConfigs()
        {
            return Ok(_service.GetActivityConfigs(GetCompanyId()));
        }
    }
}
