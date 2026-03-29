using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class SendToHourBankInput
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
