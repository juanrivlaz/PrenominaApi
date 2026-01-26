using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Filters;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Services.Excel;
using PrenominaApi.Services.Prenomina;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ServiceFilter(typeof(CompanyTenantValidationFilter))]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IBaseServicePrenomina<SysConfigReports> _service;
        private readonly ExcelReportService _excelReportService;
        public ReportsController(
            IBaseServicePrenomina<SysConfigReports> service,
            ExcelReportService excelReportService
        ) {
            _service = service;
            _excelReportService = excelReportService;
        }

        [HttpGet("delays")]
        public ActionResult<IEnumerable<ReportDelaysOutput>> GetReportDelays([FromQuery] GetReportDelays getReport)
        {
            var result = _service.ExecuteProcess<GetReportDelays, IEnumerable<ReportDelaysOutput>>(getReport);

            return Ok(result);
        }

        [HttpGet("overtimes")]
        public ActionResult<IEnumerable<ReportOvertimesOutput>> GetReportOvertimes([FromQuery] GetReportOvertimes getReport)
        {
            var result = _service.ExecuteProcess<GetReportOvertimes, IEnumerable<ReportOvertimesOutput>>(getReport);
            return Ok(result);
        }

        [HttpGet("hours-worked")]
        public ActionResult<IEnumerable<ReportHoursWorkedOutput>> GetReportHoursWorked([FromQuery] GetReportHoursWorked getReport)
        {
            var result = _service.ExecuteProcess<GetReportHoursWorked, IEnumerable<ReportHoursWorkedOutput>>(getReport);
            return Ok(result);
        }

        [HttpGet("attendance")]
        public ActionResult<IEnumerable<ReportAttendanceOutput>> GetReportAttendance([FromQuery] GetReportAttendance getReport)
        {
            var result = _service.ExecuteProcess<GetReportAttendance, IEnumerable<ReportAttendanceOutput>>(getReport);

            return Ok(result);
        }

        [HttpGet("delays/download-excel")]
        public IActionResult DownloadExcelReportDelays([FromQuery] GetReportDelays getReport)
        {
            var result = _service.ExecuteProcess<GetReportDelays, IEnumerable<ReportDelaysOutput>>(getReport);

            var excel = _excelReportService.Generate(
                ExcelReportType.ReportDelays,
                new ExcelContext { reportDelays = result }
            );

            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return this.File(
                fileContents: excel.Content,
                contentType,
                fileDownloadName: excel.FileName
            );
        }

        [HttpGet("overtimes/download-excel")]
        public IActionResult DownloadExcelReportOvertimes([FromQuery] GetReportOvertimes getReport)
        {
            var result = _service.ExecuteProcess<GetReportOvertimes, IEnumerable<ReportOvertimesOutput>>(getReport);
            var excel = _excelReportService.Generate(
                ExcelReportType.ReportOvertime,
                new ExcelContext { reportOvertimes = result }
            );

            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return this.File(
                fileContents: excel.Content,
                contentType,
                fileDownloadName: excel.FileName
            );
        }

        [HttpGet("hours-worked/download-excel")]
        public IActionResult DownloadExcelReportHoursWorked([FromQuery] GetReportHoursWorked getReport)
        {
            var result = _service.ExecuteProcess<GetReportHoursWorked, IEnumerable<ReportHoursWorkedOutput>>(getReport);
            var excel = _excelReportService.Generate(
                ExcelReportType.ReportHoursWorked,
                new ExcelContext { reportHoursWorkeds = result }
            );

            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return this.File(
                fileContents: excel.Content,
                contentType,
                fileDownloadName: excel.FileName
            );
        }

        [HttpGet("attendance/download-excel")]
        public IActionResult DownloadExcelReportAttendance([FromQuery] GetReportAttendance getReport)
        {
            var result = _service.ExecuteProcess<GetReportAttendance, IEnumerable<ReportAttendanceOutput>>(getReport);
            var excel = _excelReportService.Generate(
                ExcelReportType.ReportAttendace,
                new ExcelContext { reportAttendances = result }
            );

            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return this.File(
                fileContents: excel.Content,
                contentType,
                fileDownloadName: excel.FileName
            );
        }
    }
}
