using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrenominaApi.Filters;
using PrenominaApi.Models.Dto;
using PrenominaApi.Models.Dto.Input;
using PrenominaApi.Models.Dto.Input.Reports;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Dto.Output.Reports;
using PrenominaApi.Services.Excel;
using PrenominaApi.Services.Prenomina;
using PrenominaApi.Services.Utilities.ReportPdf;
using ClosedXML.Excel;

namespace PrenominaApi.Controllers
{
    [Route("api/[controller]"), Authorize]
    [ServiceFilter(typeof(CompanyTenantValidationFilter))]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IBaseServicePrenomina<SysConfigReports> _service;
        private readonly ExcelReportService _excelReportService;
        private readonly ReportPdfService _reportPdfService;
        private readonly OvertimeAccumulationService _overtimeAccumulationService;
        public ReportsController(
            IBaseServicePrenomina<SysConfigReports> service,
            ExcelReportService excelReportService,
            ReportPdfService reportPdfService,
            OvertimeAccumulationService overtimeAccumulationService
        )
        {
            _service = service;
            _excelReportService = excelReportService;
            _reportPdfService = reportPdfService;
            _overtimeAccumulationService = overtimeAccumulationService;
        }

        // Encabezados del reporte de horas extras (resumen por empleado).
        private static readonly string[] OvertimeSummaryHeaders =
        {
            "Codigo", "Nombre", "Departamento", "Total Extra Periodo", "Acumulado", "Pagado", "Pendiente", "Balance"
        };

        private string[] BuildOvertimeSummaryRow(Models.Dto.Output.OvertimeSummaryOutput item)
        {
            return new[]
            {
                item.EmployeeCode.ToString(),
                item.FullName,
                item.Department,
                string.IsNullOrEmpty(item.TotalOvertimeFormatted) ? FormatToTime(item.TotalOvertimeMinutes) : item.TotalOvertimeFormatted,
                FormatToTime(item.AccumulatedMinutes),
                string.IsNullOrEmpty(item.PaidMinutesFormatted) ? FormatToTime(item.PaidMinutes) : item.PaidMinutesFormatted,
                FormatToTime(item.PendingMinutes),
                string.IsNullOrEmpty(item.CurrentBalanceFormatted) ? FormatToTime(item.CurrentBalance) : item.CurrentBalanceFormatted
            };
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

        [HttpGet("incidences")]
        public ActionResult<IEnumerable<ReportIncidencesOutput>> GetIncidences([FromQuery] GetReportIncidences getReport)
        {
            var result = _service.ExecuteProcess<GetReportIncidences, IEnumerable<ReportIncidencesOutput>>(getReport);

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
        public async Task<IActionResult> DownloadExcelReportOvertimes([FromQuery] GetReportOvertimes getReport)
        {
            // Resumen por empleado (mismas columnas que la tabla en pantalla).
            var summary = await _overtimeAccumulationService.GetOvertimeSummary(
                getReport.TypeNomina,
                getReport.NumPeriod,
                (int)getReport.Company,
                getReport.Tenant,
                getReport.Search);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Horas Extras");

            for (int i = 0; i < OvertimeSummaryHeaders.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = OvertimeSummaryHeaders[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            }

            var rowIndex = 2;
            foreach (var item in summary)
            {
                var row = BuildOvertimeSummaryRow(item);
                worksheet.Cell(rowIndex, 1).Value = item.EmployeeCode;
                for (int col = 1; col < row.Length; col++)
                {
                    worksheet.Cell(rowIndex, col + 1).Value = row[col];
                }
                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return this.File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "reporte_horas_extras.xlsx"
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

        [HttpGet("incidences/download-excel")]
        public IActionResult DownloadExcelReportIncidences([FromQuery] GetReportIncidences getReport)
        {
            var result = _service.ExecuteProcess<GetReportIncidences, IEnumerable<ReportIncidencesOutput>>(getReport);
            var excel = _excelReportService.Generate(
                ExcelReportType.ReportIncidence,
                new ExcelContext { reportIncidence = result }
            );

            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return this.File(
                fileContents: excel.Content,
                contentType,
                fileDownloadName: excel.FileName
            );
        }

        [HttpGet("abandonment")]
        public ActionResult<IEnumerable<ReportAbandonmentOutput>> GetReportAbandonment([FromQuery] GetReportAbandonment getReport)
        {
            var result = _service.ExecuteProcess<GetReportAbandonment, IEnumerable<ReportAbandonmentOutput>>(getReport);
            return Ok(result);
        }

        [HttpGet("abandonment/download-excel")]
        public IActionResult DownloadExcelReportAbandonment([FromQuery] GetReportAbandonment getReport)
        {
            var result = _service.ExecuteProcess<GetReportAbandonment, IEnumerable<ReportAbandonmentOutput>>(getReport);
            var excel = _excelReportService.Generate(
                ExcelReportType.ReportAbandonment,
                new ExcelContext { reportAbandonment = result }
            );

            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return this.File(
                fileContents: excel.Content,
                contentType,
                fileDownloadName: excel.FileName
            );
        }

        // ==================== DESCARGA EN PDF ====================

        [HttpGet("delays/download-pdf")]
        public IActionResult DownloadPdfReportDelays([FromQuery] GetReportDelays getReport)
        {
            var result = _service.ExecuteProcess<GetReportDelays, IEnumerable<ReportDelaysOutput>>(getReport);

            var headers = new[] { "Codigo", "Nombre", "Departamento", "Puesto", "Fecha", "Entrada", "Salida", "Tiempo de Retardo" };
            var rows = result.Select(item => new[]
            {
                item.Code.ToString("0"),
                item.FullName,
                item.Department,
                item.JobPosition,
                item.Date.ToString("dd/MM/yyyy"),
                item.CheckIn.ToString("HH:mm"),
                item.CheckOut?.ToString("HH:mm") ?? "",
                $"{item.TimeDelayed} min"
            }).ToList();

            var pdf = _reportPdfService.Generate("Reporte de Retardos", BuildSubtitle(getReport), headers, rows);

            return this.File(pdf, "application/pdf", "reporte_retardos.pdf");
        }

        [HttpGet("overtimes/download-pdf")]
        public async Task<IActionResult> DownloadPdfReportOvertimes([FromQuery] GetReportOvertimes getReport)
        {
            // Resumen por empleado (mismas columnas que la tabla en pantalla).
            var summary = await _overtimeAccumulationService.GetOvertimeSummary(
                getReport.TypeNomina,
                getReport.NumPeriod,
                (int)getReport.Company,
                getReport.Tenant,
                getReport.Search);

            var rows = summary.Select(BuildOvertimeSummaryRow).ToList();

            var pdf = _reportPdfService.Generate("Reporte de Horas Extras", BuildSubtitle(getReport), OvertimeSummaryHeaders, rows);

            return this.File(pdf, "application/pdf", "reporte_horas_extras.pdf");
        }

        [HttpGet("hours-worked/download-pdf")]
        public IActionResult DownloadPdfReportHoursWorked([FromQuery] GetReportHoursWorked getReport)
        {
            var result = _service.ExecuteProcess<GetReportHoursWorked, IEnumerable<ReportHoursWorkedOutput>>(getReport);

            var headers = new[] { "Codigo", "Nombre", "Departamento", "Puesto", "Fecha", "Entrada", "Salida", "Horas Laboradas" };
            var rows = result.Select(item => new[]
            {
                item.Code.ToString(),
                item.FullName,
                item.Department,
                item.JobPosition,
                item.Date.ToString("dd/MM/yyyy"),
                item.CheckIn.ToString("HH:mm"),
                item.CheckOut?.ToString("HH:mm") ?? "",
                item.HoursWorked.ToString()
            }).ToList();

            var pdf = _reportPdfService.Generate("Reporte de Horas Laboradas", BuildSubtitle(getReport), headers, rows);

            return this.File(pdf, "application/pdf", "reporte_horas_laboradas.pdf");
        }

        [HttpGet("attendance/download-pdf")]
        public IActionResult DownloadPdfReportAttendance([FromQuery] GetReportAttendance getReport)
        {
            var result = _service.ExecuteProcess<GetReportAttendance, IEnumerable<ReportAttendanceOutput>>(getReport);

            var headers = new[] { "Codigo", "Nombre", "Departamento", "Puesto", "Fecha", "Entrada", "Salida" };
            var rows = result.Select(item => new[]
            {
                item.Code.ToString(),
                item.FullName,
                item.Department,
                item.JobPosition,
                item.Date.ToString("dd/MM/yyyy"),
                item.CheckIn.ToString("HH:mm"),
                item.CheckOut?.ToString("HH:mm") ?? ""
            }).ToList();

            var pdf = _reportPdfService.Generate("Reporte de Asistencia", BuildSubtitle(getReport), headers, rows);

            return this.File(pdf, "application/pdf", "reporte_asistencia.pdf");
        }

        [HttpGet("incidences/download-pdf")]
        public IActionResult DownloadPdfReportIncidences([FromQuery] GetReportIncidences getReport)
        {
            var result = _service.ExecuteProcess<GetReportIncidences, IEnumerable<ReportIncidencesOutput>>(getReport);

            var headers = new[] { "Codigo", "Nombre", "Departamento", "Puesto", "Fecha", "Incidencia", "Descripcion", "Usuario", "Fecha de Creacion" };
            var rows = result.Select(item => new[]
            {
                item.Code.ToString(),
                item.FullName,
                item.Department,
                item.JobPosition,
                item.Date.ToString("dd/MM/yyyy"),
                item.IncidenceCode,
                item.IncidenceDescription,
                item.UserFullName,
                item.CreatedAt.ToString("dd/MM/yyyy")
            }).ToList();

            var pdf = _reportPdfService.Generate("Reporte de Incidencias", BuildSubtitle(getReport), headers, rows);

            return this.File(pdf, "application/pdf", "reporte_incidencias.pdf");
        }

        [HttpGet("abandonment/download-pdf")]
        public IActionResult DownloadPdfReportAbandonment([FromQuery] GetReportAbandonment getReport)
        {
            var result = _service.ExecuteProcess<GetReportAbandonment, IEnumerable<ReportAbandonmentOutput>>(getReport);

            var headers = new[] { "Codigo", "Nombre", "Departamento", "Puesto", "Dias Consecutivos", "Fecha Inicio", "Fecha Fin" };
            var rows = result.Select(item => new[]
            {
                item.Code.ToString(),
                item.FullName,
                item.Department,
                item.JobPosition,
                item.ConsecutiveDays.ToString(),
                item.StartDate.ToString("dd/MM/yyyy"),
                item.EndDate.ToString("dd/MM/yyyy")
            }).ToList();

            var pdf = _reportPdfService.Generate("Reporte de Inasistencias", BuildSubtitle(getReport), headers, rows);

            return this.File(pdf, "application/pdf", "reporte_abandono.pdf");
        }

        private static string BuildSubtitle(GetReports getReport)
        {
            var parts = new List<string>
            {
                $"Tipo de nomina: {getReport.TypeNomina}",
                $"Periodo: {getReport.NumPeriod}"
            };

            if (getReport.FilterDates != null)
            {
                parts.Add($"Fechas: {getReport.FilterDates.Start:dd/MM/yyyy} - {getReport.FilterDates.End:dd/MM/yyyy}");
            }

            if (!string.IsNullOrWhiteSpace(getReport.Search))
            {
                parts.Add($"Busqueda: {getReport.Search}");
            }

            return string.Join("   |   ", parts);
        }

        private static string FormatToTime(int minutes)
        {
            var hours = minutes / 60;
            var mins = minutes % 60;

            return $"{hours.ToString().PadLeft(2, '0')} hrs {mins.ToString().PadLeft(2, '0')} min";
        }
    }
}
