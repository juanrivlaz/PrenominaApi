using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class GetWorkedDayOff
    {
        [Required]
        public required string DayOffId { get; set; }
        public int CompanyId { get; set; }
        public string? Tenant { get; set; }
    }
}
