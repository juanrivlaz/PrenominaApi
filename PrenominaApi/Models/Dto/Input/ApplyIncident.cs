using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class ApplyIncident
    {
        [Required]
        public required string IncidentCode { get; set; }
        [Required]
        public DateOnly Date {  get; set; }
        [Required]
        public int EmployeeCode { get; set; }
        public int CompanyId { get; set; }
        public string? UserId { get; set; }
        public decimal? Amount { get; set; }
    }
}
