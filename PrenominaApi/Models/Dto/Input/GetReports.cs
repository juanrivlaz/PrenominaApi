using PrenominaApi.Models.Dto.Input.Reports;

namespace PrenominaApi.Models.Dto.Input
{
    public class GetReports : IHasCompanyTenant
    {
        public required int TypeNomina { get; set; }
        public required int NumPeriod { get; set; }
        public required Paginator Paginator { get; set; }
        public decimal Company { get; set; }
        public string Tenant { get; set; } = string.Empty;
        public string? Search { get; set; }
        public FilterByDates? FilterDates { get; set; }
    }
}
