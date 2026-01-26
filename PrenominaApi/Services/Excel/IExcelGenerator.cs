namespace PrenominaApi.Services.Excel
{
    public interface IExcelGenerator
    {
        ExcelReportType ReportType { get; }
        GeneratedExcel Generate(ExcelContext context);
    }
}
