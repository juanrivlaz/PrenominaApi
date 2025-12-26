using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class SyncClockAttendance
    {
        [Required]
        public Guid Id { get; set; }
    }
}
