using ClosedXML.Excel;
using PrenominaApi.Models.Dto.Output;

namespace PrenominaApi.Services.Excel.Reports
{
    public class ReportHoursWorkedExcelGenerator : IExcelGenerator
    {
        public ExcelReportType ReportType => ExcelReportType.ReportHoursWorked;
        public GeneratedExcel Generate(ExcelContext context)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Hours Worked Report");
            var index = 1;

            worksheet.Cell($"A{index}").Value = "Codigo";
            worksheet.Cell($"B{index}").Value = "Nombre";
            worksheet.Cell($"C{index}").Value = "Departamento";
            worksheet.Cell($"D{index}").Value = "Puesto";
            worksheet.Cell($"E{index}").Value = "Fecha";
            worksheet.Cell($"F{index}").Value = "Checada Entrada";
            worksheet.Cell($"G{index}").Value = "Checada Salida";
            worksheet.Cell($"H{index}").Value = "Horas Laboradas";

            index++;

            foreach (var item in context.reportHoursWorkeds ?? Enumerable.Empty<ReportHoursWorkedOutput>())
            {
                worksheet.Cell($"A{index}").Value = item.Code;
                worksheet.Cell($"B{index}").Value = item.FullName;
                worksheet.Cell($"C{index}").Value = item.Department;
                worksheet.Cell($"D{index}").Value = item.JobPosition;
                worksheet.Cell($"E{index}").Value = item.Date.ToString("dd-MM-yyyy");
                worksheet.Cell($"F{index}").Value = item.CheckIn.ToString("HH:mm:ss");
                worksheet.Cell($"G{index}").Value = item.CheckOut?.ToString("HH:mm:ss") ?? "";
                worksheet.Cell($"H{index}").Value = item.HoursWorked;

                index++;
            }

            worksheet.Columns().AdjustToContents();

            return ExcelHelper.Buid(workbook, "report-hours-worked.xlsx");
        }
    }
}
