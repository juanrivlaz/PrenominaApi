using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ApiController]
    public class PeriodController : ControllerBase
    {
        private readonly IBaseServicePrenomina<Period> _service;
        private readonly GlobalPropertyService _globalPropertyService;

        public PeriodController(
            IBaseServicePrenomina<Period> service,
            GlobalPropertyService globalPropertyService
        ) {
            _service = service;
            _globalPropertyService = globalPropertyService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<Period>> Get([FromQuery] FilterPeriod filterPeriod)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            filterPeriod.CompanyId = companyId;
            filterPeriod.Year = _globalPropertyService.YearOfOperation;

            var result = new List<Period>();

            if (filterPeriod.TypePayroll > 0)
            {
                result = _service.GetByFilter((item) => item.TypePayroll == filterPeriod.TypePayroll && item.Company == filterPeriod.CompanyId && item.Year == filterPeriod.Year).ToList();
            } else
            {
                result = _service.GetAll().ToList();
            }

            return Ok(result);
        }

        [HttpGet("payrolls")]
        public ActionResult<IEnumerable<Models.Payroll>> GetPayrolls()
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            var result = _service.ExecuteProcess<int, IEnumerable<Models.Payroll>>(companyId);

            return Ok(result);
        }

        [HttpPost]
        public ActionResult<IEnumerable<Period>> Store([FromBody] CreatePeriods createPeriods)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            createPeriods.CompanyId = companyId;
            createPeriods.Year = _globalPropertyService.YearOfOperation;
            var result = _service.ExecuteProcess<CreatePeriods, IEnumerable<Period>>(createPeriods);

            return Ok(result);
        }

        [HttpPost("by-file")]
        public async Task<ActionResult<IEnumerable<Period>>> StoreByFileAsync([FromForm] CreatePeriodsByFile createPeriods)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            createPeriods.CompanyId = companyId;
            createPeriods.Year = _globalPropertyService.YearOfOperation;
            var result = await _service.ExecuteProcess<CreatePeriodsByFile, Task<IEnumerable<Period>>>(createPeriods);

            return Ok(result);
        }

        [HttpPost("change-status")]
        public ActionResult<IEnumerable<PeriodStatus>> ChangeStatus([FromBody] ChangePeriodStatus changePeriodStatus)
        {
            var userId = HttpContext.User.FindFirst("UserId")?.Value;
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            changePeriodStatus.CompanyId = companyId;
            changePeriodStatus.Year = _globalPropertyService.YearOfOperation;
            changePeriodStatus.ByUserId = userId;

            var result = _service.ExecuteProcess<ChangePeriodStatus, IEnumerable<PeriodStatus>>(changePeriodStatus);

            return Ok(result);
        }
    }
}
