using PrenominaApi.Converters;
using System.Text.Json.Serialization;

namespace PrenominaApi.Models.Dto.Input
{
    public class GetAttendanceEmployees
    {
        public required int TypeNomina { get; set; }
        public required int NumPeriod { get; set; }
        public required Paginator Paginator { get; set; }
        public decimal Company { get; set; }
        public string? Tenant { get; set; }
        public string? Search {  get; set; }
        public string? OrderBy { get; set; }

    }
}
