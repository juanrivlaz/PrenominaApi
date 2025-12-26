using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input
{
    public class GetWorkedSunday
    {
        [Required]
        public required int PayrollId { get; set; }
        [Required]
        public required int NumberPeriod { get; set; }
        public int CompanyId { get; set; }
        public string? Tenant { get; set; }
    }
}
