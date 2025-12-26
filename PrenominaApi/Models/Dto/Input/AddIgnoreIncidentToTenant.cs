namespace PrenominaApi.Models.Dto.Input
{
    public class AddIgnoreIncidentToTenant
    {
        public required string TenantId { get; set; }
        public required IEnumerable<IgnoreIncident> IncidentCodes { get; set; }
    }
}
