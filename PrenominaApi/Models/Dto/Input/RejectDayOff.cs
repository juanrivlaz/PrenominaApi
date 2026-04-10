namespace PrenominaApi.Models.Dto.Input
{
    public class RejectDayOff
    {
        public required int EmployeeCode { get; set; }
        public required DateOnly Date { get; set; }
        public string? Comment { get; set; }
        public decimal CompanyId { get; set; }
        public string? UserId { get; set; }
    }
}
