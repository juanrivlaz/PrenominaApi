using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input.Attendance
{
    public class ChangeAttendance
    {
        [Required]
        public decimal EmployeeCode { get; set; }
        [Required]
        public DateOnly Date { get; set; }
        [Required]
        public required string CheckEntry { get; set; }
        [Required]
        public required string CheckOut { get; set; }
        public string? CheckEntryId { get; set; } = null;
        public string? CheckOutId { get; set; } = null;
        public string? UserId { get; set; } = null;
        public decimal CompanyId { get; set; }
    }
}
