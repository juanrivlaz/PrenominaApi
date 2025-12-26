namespace PrenominaApi.Models.Dto.Input
{
    public class CreateRole
    {
        public required string Label { get; set; }
        public required IEnumerable<CreateSection> Sections { get; set; }
        public required bool CanClosePayrollPeriod { get; set; }
    }
}
