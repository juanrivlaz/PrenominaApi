using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Services;
using PrenominaApi.Services.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]"), Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IBaseService<AttendanceRecords> _service;
        private readonly IBaseServicePrenomina<AssistanceIncident> _assistanceIncidentService;

        public AttendanceController(
            IBaseService<AttendanceRecords> service,
            IBaseServicePrenomina<AssistanceIncident> assistanceIncidentService
        ) {
            _service = service;
            _assistanceIncidentService = assistanceIncidentService;
        }

        [HttpGet]
        public ActionResult<PagedResult<EmployeeAttendancesOutput>> Get([FromQuery] GetAttendanceEmployees filter)
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";

            filter.Company = Convert.ToDecimal(company);
            filter.Tenant = tenant;

            var result = _service.ExecuteProcess<GetAttendanceEmployees, PagedResult<EmployeeAttendancesOutput>>(filter);
            
            return Ok(result);
        }

        [HttpGet("init")]
        public ActionResult<InitAttendanceRecords> GetInit()
        {
            string? headerCompany = HttpContext.Items["companySelected"]?.ToString();
            var company = int.Parse(headerCompany ?? "0");
            var result = _service.ExecuteProcess<int, InitAttendanceRecords>(company);

            return Ok(result);
        }

        [HttpGet("download")]
        public IActionResult Download([FromQuery] DownloadAttendanceEmployee downloadAttendance)
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";

            downloadAttendance.Company = Convert.ToDecimal(company);
            downloadAttendance.Tenant = tenant;

            var result = _service.ExecuteProcess<DownloadAttendanceEmployee, byte[]>(downloadAttendance);
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = "incident_code.xlsx";

            if (downloadAttendance.TypeFileDownload == TypeFileDownload.PDF)
            {
                contentType = "application/pdf";
                fileName = "attendace_employees.pdf";
            }

            return this.File(
                fileContents: result,
                contentType,
                fileDownloadName: fileName
            );
        }

        [HttpPatch("apply-incident")]
        public ActionResult<AssistanceIncident> ApplyIncident([FromBody] ApplyIncident applyIncident)
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

            applyIncident.UserId = userId;
            applyIncident.CompanyId = companyId;

            var result = _assistanceIncidentService.ExecuteProcess<ApplyIncident, AssistanceIncident>(applyIncident);

            return Ok(result);
        }

        [HttpDelete("delete-incidents")]
        public ActionResult<bool> DeleteIncidents([FromBody] DeleteIncidentsToEmployee deleteIncidentsToEmployee)
        {
            string userId = HttpContext.User.FindFirst("UserId")?.Value ?? "";

            if (String.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Unauthorized");
            }

            deleteIncidentsToEmployee.UserId = userId;
            var result = _assistanceIncidentService.ExecuteProcess<DeleteIncidentsToEmployee, bool>(deleteIncidentsToEmployee);

            return Ok(result);
        }

        [HttpPost("assign-double-shift")]
        public ActionResult<AssistanceIncident> AssignDoubleShift([FromBody] AssignDoubleShift assignDoubleShift)
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

            assignDoubleShift.UserId = userId;
            assignDoubleShift.CompanyId = companyId;

            var result = _assistanceIncidentService.ExecuteProcess<AssignDoubleShift, AssistanceIncident>(assignDoubleShift);

            return Ok(result);
        }
    }
}
