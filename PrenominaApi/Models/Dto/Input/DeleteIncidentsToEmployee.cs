using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class DeleteIncidentsToEmployee
    {
        [Required]
        public required IEnumerable<string> IncidentIds { get; set; }
        public string? UserId { get; set; }
    }
}
