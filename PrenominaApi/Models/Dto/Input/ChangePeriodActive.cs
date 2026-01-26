namespace PrenominaApi.Models.Dto.Input
{
    public class ChangePeriodActive
    {
        public required string PeriodId { get; set; }
        public required bool IsActive { get; set; }
        public string? ByUserId { get; set; }
    }
}
