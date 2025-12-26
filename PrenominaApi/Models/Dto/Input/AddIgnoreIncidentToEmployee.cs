namespace PrenominaApi.Models.Dto.Input
{
    public class AddIgnoreIncidentToEmployee
    {
        public required int EmployeeCode { get; set; }
        public required IEnumerable<IgnoreIncident> IncidentCodes { get; set; }
    }
}
