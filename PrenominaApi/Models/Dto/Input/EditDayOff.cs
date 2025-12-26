using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class EditDayOff : CreateDayOff
    {
        [Required]
        public Guid Id { get; set; }
    }
}
