namespace PrenominaApi.Models.Dto.Input
{
    public class AddIgnoreIncidentToActivity
    {
        public required int ActivityId { get; set; }
        public required IEnumerable<IgnoreIncident> IncidentCodes { get; set; }
    }
}
