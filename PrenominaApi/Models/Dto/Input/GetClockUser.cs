using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class GetClockUser
    {
        [Required]
        public Guid Id { get; set; }
    }
}
