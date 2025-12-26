using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class CreateDayOff
    {
        [Required]
        public DateOnly Date { get; set; }
        [Required]
        public required string Description { get; set; }
        [Required]
        public required string IncidentCode { get; set; }
        [Required]
        public bool IsUnion { get; set; } = false;
    }
}
