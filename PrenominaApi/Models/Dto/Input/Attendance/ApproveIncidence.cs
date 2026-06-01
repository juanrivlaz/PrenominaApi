namespace PrenominaApi.Models.Dto.Input.Attendance
{
    /// <summary>
    /// Registra la aprobación del usuario actual sobre una incidencia. La incidencia sólo queda
    /// aprobada (Approved = true) cuando TODOS los aprobadores configurados la han aprobado.
    /// </summary>
    public class ApproveIncidence
    {
        public required Guid AssistanceIncidentId { get; set; }
        public int CompanyId { get; set; }
        public string? UserId { get; set; }
    }
}
