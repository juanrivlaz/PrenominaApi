using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class ManualOvertimeEntryInput
    {
        [Required]
        public int EmployeeCode { get; set; }

        [Required]
        public DateOnly SourceDate { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Los minutos deben ser mayores a 0")]
        public int Minutes { get; set; }

        public string? Notes { get; set; }

        public string? ExternalReference { get; set; }
    }
}
