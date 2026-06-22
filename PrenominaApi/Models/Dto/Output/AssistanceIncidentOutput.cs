namespace PrenominaApi.Models.Dto.Output
{
    public class AssistanceIncidentOutput
    {
        public Guid Id { get; set; }
        public DateOnly Date {  get; set; }
        public required string IncidentCode { get; set; }
        public bool TimeOffRequest { get; set; }
        // True cuando la incidencia proviene de un flujo de aprobación (solicitud de ausencia o
        // incidencia que requiere aprobación); no debe poder editarse ni eliminarse desde asistencia.
        public bool FromApprovalFlow { get; set; }
        public bool Approved { get; set; }
        public string? Label { get; set; }
        public bool IsAdditional { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
