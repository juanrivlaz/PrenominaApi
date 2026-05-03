using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class BlockOnClocksInput
    {
        [Required]
        public bool Blocked { get; set; }
    }
}
