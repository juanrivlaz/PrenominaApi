using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class SyncClockUserToDB
    {
        [Required]
        public Guid Id { get; set; }
    }
}
