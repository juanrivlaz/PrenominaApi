namespace PrenominaApi.Services.Excel
{
    public class ExcelGeneratorFactory : IExcelGeneratorFactory
    {
        private readonly IReadOnlyDictionary<ExcelReportType, IExcelGenerator> _generator;
        public ExcelGeneratorFactory(IEnumerable<IExcelGenerator> excelGenerators)
        {
            _generator = excelGenerators.ToDictionary(g => g.ReportType);
        }

        public IExcelGenerator Get(ExcelReportType reportType)
        {
            if (!_generator.TryGetValue(reportType, out var generator))
            {
                throw new NotSupportedException($"The report type '{reportType}' is not supported.");
            }

            return generator;
        }
    }
}
