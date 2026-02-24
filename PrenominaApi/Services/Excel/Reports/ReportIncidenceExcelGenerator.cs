using ClosedXML.Excel;
using PrenominaApi.Models.Dto.Output;
using PrenominaApi.Models.Dto.Output.Reports;

namespace PrenominaApi.Services.Excel.Reports
{
    public class ReportIncidenceExcelGenerator : IExcelGenerator
    {
        public ExcelReportType ReportType => ExcelReportType.ReportIncidence;

        public GeneratedExcel Generate(ExcelContext context)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Incidence Report");
            var index = 1;
            worksheet.Cell($"A{index}").Value = "Codigo";
            worksheet.Cell($"B{index}").Value = "Nombre";
            worksheet.Cell($"C{index}").Value = "Departamento";
            worksheet.Cell($"D{index}").Value = "Puesto";
            worksheet.Cell($"E{index}").Value = "Fecha";
            worksheet.Cell($"F{index}").Value = "Incidencia";
            worksheet.Cell($"G{index}").Value = "Descripción";
            worksheet.Cell($"H{index}").Value = "Usuario";
            worksheet.Cell($"I{index}").Value = "Fecha de Creación";
            index++;

            foreach (var item in context.reportIncidence ?? Enumerable.Empty<ReportIncidencesOutput>())
            {
                worksheet.Cell($"A{index}").Value = item.Code;
                worksheet.Cell($"B{index}").Value = item.FullName;
                worksheet.Cell($"C{index}").Value = item.Department;
                worksheet.Cell($"D{index}").Value = item.JobPosition;
                worksheet.Cell($"E{index}").Value = item.Date.ToString("dd-MM-yyyy");
                worksheet.Cell($"F{index}").Value = item.IncidenceCode;
                worksheet.Cell($"G{index}").Value = item.IncidenceDescription;
                worksheet.Cell($"H{index}").Value = item.UserFullName;
                worksheet.Cell($"I{index}").Value = item.CreatedAt.ToString("dd-MM-yyyy");

                index++;
            }

            worksheet.Columns().AdjustToContents();

            return ExcelHelper.Buid(workbook, "report-incidence.xlsx");
        }
    }
}
