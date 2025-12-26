using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class ClockInterval
    {
        [Required]
        public required int Minutes { get; set; }
    }
}
