namespace PrenominaApi.Models.Dto.Output
{
    public class AssistanceIncidentOutput
    {
        public Guid Id { get; set; }
        public DateOnly Date {  get; set; }
        public required string IncidentCode { get; set; }
        public bool TimeOffRequest { get; set; }
        public bool Approved { get; set; }
        public string? Label { get; set; }
        public bool IsAdditional { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
