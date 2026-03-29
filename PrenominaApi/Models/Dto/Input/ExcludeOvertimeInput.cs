using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class ExcludeOvertimeInput
    {
        [Required]
        public bool ExcludeOvertime { get; set; }
    }
}
