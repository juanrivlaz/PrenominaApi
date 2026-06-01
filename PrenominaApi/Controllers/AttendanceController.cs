using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Models;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Input.Attendance;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Dto.Output.Attendance;
using PrenominaApi.Models.Prenomina;
using PrenominaApi.Models.Prenomina.Enums;
using PrenominaApi.Filters;
using PrenominaApi.Services;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]"), Authorize]
    [ServiceFilter(typeof(CompanyTenantValidationFilter))]
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

        [HttpGet("additional-pay")]
        public ActionResult<IEnumerable<AdditionalPay>> GetAdditionalPay([FromQuery] GetAdditionalPay additionalPay)
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";
            additionalPay.Company = Convert.ToDecimal(company);
            additionalPay.Tenant = tenant;

            var result = _service.ExecuteProcess<GetAdditionalPay, IEnumerable<AdditionalPay>>(additionalPay);

            return Ok(result);
        }

        [HttpGet("additional-pay/download")]
        public IActionResult DownloadAdditionalPay([FromQuery] DownloadAdditionalPay downloadAdditionalPay)
        {
            var company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            var tenant = HttpContext.Items["tenantSelected"]?.ToString() ?? "";
            
            downloadAdditionalPay.Company = Convert.ToDecimal(company);
            downloadAdditionalPay.Tenant = tenant;

            var result = _service.ExecuteProcess<DownloadAdditionalPay, byte[]>(downloadAdditionalPay);
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = "additional_pay.xlsx";

            if (downloadAdditionalPay.TypeFileDownload == TypeFileDownload.PDF)
            {
                contentType = "application/pdf";
                fileName = "additional_pay.pdf";
            }

            return this.File(
                fileContents: result,
                contentType,
                fileDownloadName: fileName
            );
        }

        [HttpGet("additional-pay/template")]
        public IActionResult DownloadAdditionalPayTemplate()
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Pagos Adicionales");

            // Headers
            var headers = new[] { "EmployeeCode", "IncidentCode", "Date (dd/MM/yyyy)", "BaseValue", "OperationValue", "Notes" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
            }

            // Fila ejemplo
            ws.Cell(2, 1).Value = 93;
            ws.Cell(2, 2).Value = "BONO";
            ws.Cell(2, 3).Value = DateTime.Now.ToString("dd/MM/yyyy");
            ws.Cell(2, 4).Value = 0;
            ws.Cell(2, 5).Value = 0;
            ws.Cell(2, 6).Value = "Ejemplo: bono de productividad";

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "plantilla_pagos_adicionales.xlsx");
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

        [HttpPatch("change")]
        public ActionResult<bool> ChangeAttendance([FromBody] ChangeAttendance changeAttendance)
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

            changeAttendance.UserId = userId;
            changeAttendance.CompanyId = companyId;

            var result = _assistanceIncidentService.ExecuteProcess<ChangeAttendance, bool>(changeAttendance);

            return Ok(result);
        }

        [HttpPatch("delete-checkins")]
        public ActionResult<bool> DeleteCheckins([FromBody] DeleteCheckins deleteCheckins)
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

            deleteCheckins.UserId = userId;
            deleteCheckins.CompanyId = companyId;

            var result = _assistanceIncidentService.ExecuteProcess<DeleteCheckins, bool>(deleteCheckins);

            return Ok(result);
        }

        [HttpGet("pending-incidence-approvals")]
        public ActionResult<IEnumerable<PendingIncidenceApprovalOutput>> GetPendingIncidenceApprovals()
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

            var result = _assistanceIncidentService.ExecuteProcess<GetPendingIncidenceApprovals, IEnumerable<PendingIncidenceApprovalOutput>>(new GetPendingIncidenceApprovals
            {
                CompanyId = companyId,
                UserId = userId
            });

            return Ok(result);
        }

        [HttpPost("approve-incidence")]
        public ActionResult<AssistanceIncident> ApproveIncidence([FromBody] ApproveIncidence approveIncidence)
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

            approveIncidence.CompanyId = companyId;
            approveIncidence.UserId = userId;

            var result = _assistanceIncidentService.ExecuteProcess<ApproveIncidence, AssistanceIncident>(approveIncidence);

            return Ok(result);
        }

        [HttpPost("reject-incidence")]
        public ActionResult<AssistanceIncident> RejectIncidence([FromBody] RejectIncidence rejectIncidence)
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

            rejectIncidence.CompanyId = companyId;
            rejectIncidence.UserId = userId;

            var result = _assistanceIncidentService.ExecuteProcess<RejectIncidence, AssistanceIncident>(rejectIncidence);

            return Ok(result);
        }

        [HttpPost("fix-night-shift-eos")]
        public ActionResult<FixNightShiftEoSResult> FixNightShiftEoS()
        {
            string company = HttpContext.Items["companySelected"]?.ToString() ?? "";
            int companyId = int.Parse(company ?? "0");

            if (companyId <= 0)
            {
                throw new BadHttpRequestException("Es necesario seleccionar una empresa");
            }

            var result = _assistanceIncidentService.ExecuteProcess<FixNightShiftEoS, FixNightShiftEoSResult>(new FixNightShiftEoS
            {
                CompanyId = companyId
            });

            return Ok(result);
        }
    }
}
