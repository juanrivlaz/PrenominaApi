using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class RegisterDaysOff
    {
        [Required]
        public required IEnumerable<DateOnly> Dates { get; set; }
        [Required]
        public required decimal EmployeeCode { get; set; }
        [Required]
        public required string IncidentCode { get; set; }
        public bool RequireAbsenceRequest { get; set; }
        public decimal CompanyId { get; set; }
        public string? UserId { get; set; }
        public string? Notes { get; set; }
    }
}
