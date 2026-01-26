namespace PrenominaApi.Models.Dto.Output
{
    public class AttendanceOutput
    {
        public DateOnly Date { get; set; }
        public required string IncidentCode { get; set; }
        public required string IncidentCodeLabel { get; set; }
        public int? TypeNom { get; set; }
        public Guid? CheckEntryId { get; set; }
        public string? CheckEntry { get; set; }
        public Guid? CheckOutId { get; set; }
        public string? CheckOut { get; set; }
        public IEnumerable<AssistanceIncidentOutput>? AssistanceIncidents { get; set; }
    }
}
