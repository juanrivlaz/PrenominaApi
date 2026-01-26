namespace PrenominaApi.Services.Excel
{
    public interface IExcelGeneratorFactory
    {
        IExcelGenerator Get(ExcelReportType reportType);
    }
}
