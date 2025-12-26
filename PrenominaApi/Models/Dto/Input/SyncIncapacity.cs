namespace PrenominaApi.Models.Dto.Input
{
    public class SyncIncapacity
    {
        public required string PeriodId { get; set; }
        public int CompanyId { get; set; }
        public required string TenantId { get; set; }
        public int TypeNom { get; set; }
        public string? UserId { get; set; }
    }
}
