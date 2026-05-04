namespace PrenominaApi.Models.Dto.Input
{
    public class EditPeriod
    {
        public string? Id { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly ClosingDate { get; set; }
        public DateOnly DatePayment { get; set; }
        public DateOnly StartAdminDate { get; set; }
        public DateOnly ClosingAdminDate { get; set; }
        public int CompanyId { get; set; }
    }
}
