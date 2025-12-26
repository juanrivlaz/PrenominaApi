namespace PrenominaApi.Models.Dto.Input
{
    public class VerifyClosedPeriod
    {
        public required int CompanyId { get; set; }
        public required int Year { get; set; }
        public required int TypePayroll { get; set; }
        public required string TenantId { get; set; }
        public required int NumPeriod { get; set; }
    }
}
