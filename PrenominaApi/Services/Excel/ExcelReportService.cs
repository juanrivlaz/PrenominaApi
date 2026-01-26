namespace PrenominaApi.Services.Excel
{
    public class ExcelReportService
    {
        private readonly IExcelGeneratorFactory _factory;
        public ExcelReportService(IExcelGeneratorFactory factory)
        {
            _factory = factory;
        }

        public GeneratedExcel Generate(ExcelReportType reportType, ExcelContext context)
        {
            var generator = _factory.Get(reportType);

            return generator.Generate(context);
        }

        public IReadOnlyList<GeneratedExcel> GenerateMany(
            IEnumerable<ExcelReportType> reportTypes,
            ExcelContext context
        )
        {
            var result = new List<GeneratedExcel>();

            foreach (var reportType in reportTypes)
            {
                result.Add(Generate(reportType, context));
            }

            return result;
        }
    }
}
