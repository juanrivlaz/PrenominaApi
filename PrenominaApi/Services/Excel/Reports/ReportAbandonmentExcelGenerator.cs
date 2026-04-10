using ClosedXML.Excel;
using PrenominaApi.Models.Dto.Output;

namespace PrenominaApi.Services.Excel.Reports
{
    public class ReportAbandonmentExcelGenerator : IExcelGenerator
    {
        public ExcelReportType ReportType => ExcelReportType.ReportAbandonment;

        public GeneratedExcel Generate(ExcelContext context)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Abandono de Trabajo");
            var index = 1;
            worksheet.Cell($"A{index}").Value = "Codigo";
            worksheet.Cell($"B{index}").Value = "Nombre";
            worksheet.Cell($"C{index}").Value = "Departamento";
            worksheet.Cell($"D{index}").Value = "Puesto";
            worksheet.Cell($"E{index}").Value = "Días Consecutivos";
            worksheet.Cell($"F{index}").Value = "Fecha Inicio";
            worksheet.Cell($"G{index}").Value = "Fecha Fin";
            index++;

            foreach (var item in context.reportAbandonment ?? Enumerable.Empty<ReportAbandonmentOutput>())
            {
                worksheet.Cell($"A{index}").Value = item.Code;
                worksheet.Cell($"B{index}").Value = item.FullName;
                worksheet.Cell($"C{index}").Value = item.Department;
                worksheet.Cell($"D{index}").Value = item.JobPosition;
                worksheet.Cell($"E{index}").Value = item.ConsecutiveDays;
                worksheet.Cell($"F{index}").Value = item.StartDate.ToString("dd-MM-yyyy");
                worksheet.Cell($"G{index}").Value = item.EndDate.ToString("dd-MM-yyyy");

                index++;
            }

            worksheet.Columns().AdjustToContents();

            return ExcelHelper.Buid(workbook, "report-abandonment.xlsx");
        }
    }
}
