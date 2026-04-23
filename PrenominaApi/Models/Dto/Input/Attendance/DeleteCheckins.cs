namespace PrenominaApi.Models.Dto.Input.Attendance
{
    public class DeleteCheckins
    {
        public string? CheckEntryId { get; set; }
        public string? CheckOutId { get; set; }
        public string? UserId { get; set; }
        public decimal CompanyId { get; set; }
    }
}
