using System.ComponentModel.DataAnnotations;
using PrenominaApi.Models.Prenomina;

namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Input para acumular horas extras
    /// </summary>
    public class AccumulateOvertimeInput
    {
        [Required]
        public int EmployeeCode { get; set; }

        [Required]
        public DateOnly SourceDate { get; set; }

        [Required]
        public int Minutes { get; set; }

        public TimeOnly? CheckIn { get; set; }

        public TimeOnly? CheckOut { get; set; }

        public string? Notes { get; set; }
    }
}
