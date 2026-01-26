
using ClosedXML.Excel;
using PrenominaApi.Models.Dto.Output;

namespace PrenominaApi.Services.Excel.Reports
{
    public class ReportOvertimeExcelGenerator : IExcelGenerator
    {
        public ExcelReportType ReportType => ExcelReportType.ReportOvertime;

        public GeneratedExcel Generate(ExcelContext context)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Overtime Report");
            var index = 1;

            worksheet.Cell($"A{index}").Value = "Codigo";
            worksheet.Cell($"B{index}").Value = "Nombre";
            worksheet.Cell($"C{index}").Value = "Departamento";
            worksheet.Cell($"D{index}").Value = "Puesto";
            worksheet.Cell($"E{index}").Value = "Fecha";
            worksheet.Cell($"F{index}").Value = "Checada Entrada";
            worksheet.Cell($"G{index}").Value = "Checada Salida";
            worksheet.Cell($"H{index}").Value = "Tiempo Extra";

            index++;

            foreach (var item in context.reportOvertimes ?? Enumerable.Empty<ReportOvertimesOutput>())
            {
                worksheet.Cell($"A{index}").Value = item.Code;
                worksheet.Cell($"B{index}").Value = item.FullName;
                worksheet.Cell($"C{index}").Value = item.Department;
                worksheet.Cell($"D{index}").Value = item.JobPosition;
                worksheet.Cell($"E{index}").Value = item.Date.ToString("dd-MM-yyyy");
                worksheet.Cell($"F{index}").Value = item.CheckIn.ToString("HH:mm:ss");
                worksheet.Cell($"G{index}").Value = item.CheckOut?.ToString("HH:mm:ss") ?? "";
                worksheet.Cell($"H{index}").Value = FormatToTime(item.Overtime - (8 * 60));

                index++;
            }

            worksheet.Columns().AdjustToContents();

            return ExcelHelper.Buid(workbook, "report-overtime.xlsx");
        }

        private string FormatToTime(int mins)
        {
            var hours = mins / 60;
            var minutes = mins % 60;

            return $"{hours.ToString().PadLeft(2, '0')} hrs {minutes.ToString().PadLeft(2, '0')} min";
        }
    }
}
