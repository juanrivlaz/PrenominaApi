namespace PrenominaApi.Models.Dto.Output.Attendance
{
    /// <summary>
    /// Incidencia pendiente de aprobación mostrada en la bandeja de aprobaciones.
    /// </summary>
    public class PendingIncidenceApprovalOutput
    {
        public Guid Id { get; set; }
        public int EmployeeCode { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string IncidentCode { get; set; } = string.Empty;
        public string IncidentDescription { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        // Progreso de aprobación: cuántos aprobadores se requieren y cuántos ya aprobaron.
        public int TotalApprovers { get; set; }
        public int ApprovedCount { get; set; }
        public bool AlreadyApprovedByMe { get; set; }
    }
}
