using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class CreateClock
    {
        [Required]
        public required string Label { get; set; }
        [Required]
        [RegularExpression(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$", ErrorMessage = "Invalid IP address format")]
        public required string Ip {  get; set; }
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        public int? Port { get; set; } = 4370;
    }
}
