namespace PrenominaApi.Models.Dto.Output.Attendance
{
    /// <summary>
    /// Incidencia pendiente de aprobación mostrada en la bandeja de aprobaciones.
    /// </summary>
    public class PendingIncidenceApprovalOutput
    {
        public Guid Id { get; set; }
        // Identificador del grupo de permiso (mismo valor para incidencias registradas juntas
        // desde el menú de permisos en varios días). Null cuando es una incidencia individual.
        public Guid? RequestGroupId { get; set; }
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
        // Estado actual de la incidencia (para distinguir aprobadas/rechazadas/pendientes al filtrar).
        public bool Approved { get; set; }
        public bool Rejected { get; set; }
    }
}
