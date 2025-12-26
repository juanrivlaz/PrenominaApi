using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DayOffsController : ControllerBase
    {
        private readonly IBaseServicePrenomina<DayOff> _service;
        private readonly GlobalPropertyService _globalPropertyService;

        public DayOffsController(IBaseServicePrenomina<DayOff> service, GlobalPropertyService globalPropertyService)
        {
            _service = service;
            _globalPropertyService = globalPropertyService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<DayOff>> Get()
        {
            var result = _service.GetAll();

            return Ok(result);
        }

        [HttpGet("get-employees")]
        public ActionResult<PagedResult<EmployeeDayOffOutput>> GetEmployee([FromQuery] FilterEmployeesByPayroll filter)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");
            TypeTenant typeTenant = _globalPropertyService.TypeTenant;

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            if (String.IsNullOrEmpty(tenant))
            {
                throw new BadHttpRequestException($"Es necesario seleccionar un {(typeTenant == TypeTenant.Department ? "departamento" : "supervisor")}");
            }

            filter.CompanyId = companyId;
            filter.Tenant = tenant;

            var result = _service.ExecuteProcess<FilterEmployeesByPayroll, PagedResult<EmployeeDayOffOutput>>(filter);

            return Ok(result);
        }

        [HttpGet("worked-days")]
        public ActionResult<IEnumerable<WorkedDayOffs>> GetWorkedDays([FromQuery] GetWorkedDayOff getWorkedDayOff)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");
            TypeTenant typeTenant = _globalPropertyService.TypeTenant;

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            if (String.IsNullOrEmpty(tenant))
            {
                throw new BadHttpRequestException($"Es necesario seleccionar un {(typeTenant == TypeTenant.Department ? "departamento" : "supervisor")}");
            }

            getWorkedDayOff.CompanyId = companyId;
            getWorkedDayOff.Tenant = tenant;

            var result = _service.ExecuteProcess<GetWorkedDayOff, IEnumerable<WorkedDayOffs>>(getWorkedDayOff);

            return Ok(result);
        }

        [HttpGet("worked-sunday")]
        public ActionResult<IEnumerable<WorkedDayOffs>> GetWorkedSunday([FromQuery] GetWorkedSunday getWorkedSunday)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");
            TypeTenant typeTenant = _globalPropertyService.TypeTenant;

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            if (String.IsNullOrEmpty(tenant))
            {
                throw new BadHttpRequestException($"Es necesario seleccionar un {(typeTenant == TypeTenant.Department ? "departamento" : "supervisor")}");
            }

            getWorkedSunday.CompanyId = companyId;
            getWorkedSunday.Tenant = tenant;

            var result = _service.ExecuteProcess<GetWorkedSunday, IEnumerable<WorkedDayOffs>>(getWorkedSunday);

            return Ok(result);
        }

        [HttpGet("worked-sunday/download")]
        public IActionResult GenerateIncidentFile([FromQuery] DownloadWorkedSunday downloadWorkedSunday)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");
            TypeTenant typeTenant = _globalPropertyService.TypeTenant;

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            if (String.IsNullOrEmpty(tenant))
            {
                throw new BadHttpRequestException($"Es necesario seleccionar un {(typeTenant == TypeTenant.Department ? "departamento" : "supervisor")}");
            }

            downloadWorkedSunday.CompanyId = companyId;
            downloadWorkedSunday.Tenant = tenant;

            var result = _service.ExecuteProcess<DownloadWorkedSunday, byte[]>(downloadWorkedSunday);
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = "worked-sunday.xlsx";

            if (downloadWorkedSunday.TypeFileDownload == TypeFileDownload.PDF)
            {
                contentType = "application/pdf";
                fileName = "worked-sunday.pdf";
            }

            return this.File(
                fileContents: result,
                contentType,
                fileDownloadName: fileName
            );
        }

        [HttpGet("worked-days/download")]
        public IActionResult GenerateIncidentFile([FromQuery] DownloadWorkedDayoff downloadWorkedDayoff)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");
            TypeTenant typeTenant = _globalPropertyService.TypeTenant;

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            if (String.IsNullOrEmpty(tenant))
            {
                throw new BadHttpRequestException($"Es necesario seleccionar un {(typeTenant == TypeTenant.Department ? "departamento" : "supervisor")}");
            }

            downloadWorkedDayoff.CompanyId = companyId;
            downloadWorkedDayoff.Tenant = tenant;

            var result = _service.ExecuteProcess<DownloadWorkedDayoff, byte[]>(downloadWorkedDayoff);
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = "worked-sunday.xlsx";

            if (downloadWorkedDayoff.TypeFileDownload == TypeFileDownload.PDF)
            {
                contentType = "application/pdf";
                fileName = "worked-sunday.pdf";
            }

            return this.File(
                fileContents: result,
                contentType,
                fileDownloadName: fileName
            );
        }

        [HttpPost]
        public ActionResult<DayOff> Store([FromBody] CreateDayOff createDayOff)
        {
            var result = _service.ExecuteProcess<CreateDayOff, DayOff>(createDayOff);

            return Ok(result);
        }

        [HttpPut]
        public ActionResult<DayOff> Update([FromBody] EditDayOff editDayOff)
        {
            var result = _service.ExecuteProcess<EditDayOff, DayOff>(editDayOff);

            return Ok(result);
        }

        [HttpDelete("{dayOffId}")]
        public ActionResult<DayOff> Delete(string dayOffId)
        {
            var result = _service.ExecuteProcess<DeleteDayOff, DayOff>(new DeleteDayOff() { Id = dayOffId });

            return Ok(result);
        }

        [HttpPost("register-to-user")]
        public ActionResult<EmployeeDayOffOutput> RegisterDayOff([FromBody] RegisterDaysOff registerDaysOff)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            string userId = HttpContext.User.FindFirst("UserId")?.Value ?? "";
            int companyId = int.Parse(company ?? "0");
            TypeTenant typeTenant = _globalPropertyService.TypeTenant;

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }
            else if (String.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            registerDaysOff.CompanyId = companyId;
            registerDaysOff.UserId = userId;

            var result = _service.ExecuteProcess<RegisterDaysOff, EmployeeDayOffOutput>(registerDaysOff);

            return Ok(result);
        }

        [HttpPost("sync-incapacity")]
        public ActionResult<SyncIncapacityOutput> SyncIncapacity([FromBody] SyncIncapacity syncIncapacity)
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            string userId = HttpContext.User.FindFirst("UserId")?.Value ?? "";
            int companyId = int.Parse(company ?? "0");

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }
            else if (String.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            syncIncapacity.CompanyId = companyId;
            syncIncapacity.UserId = userId;

            var result = _service.ExecuteProcess<SyncIncapacity, SyncIncapacityOutput>(syncIncapacity);

            return Ok(result);
        }
    }
}
