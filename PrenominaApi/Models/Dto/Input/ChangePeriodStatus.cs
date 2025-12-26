namespace PrenominaApi.Models.Dto.Input
{
    public class ChangePeriodStatus
    {
        public int CompanyId { get; set; }
        public int Year { get; set; }
        public string? ByUserId { get; set; }
        public required int TypePayroll { get; set; }
        public required string TenantId { get; set; }
        public required int NumPeriod { get; set; }
    }
}
