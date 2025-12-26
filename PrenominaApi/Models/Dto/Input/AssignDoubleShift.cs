using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class AssignDoubleShift
    {
        [Required]
        public DateOnly Date { get; set; }
        [Required]
        public int EmployeeCode { get; set; }
        public int CompanyId { get; set; }
        public string? UserId { get; set; }
    }
}
