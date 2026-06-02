namespace PrenominaApi.Models.Dto.Input.Attendance
{
    /// <summary>
    /// Lista las incidencias pendientes de aprobación que el usuario actual puede aprobar
    /// (es decir, donde está configurado como aprobador del código de incidencia).
    /// </summary>
    public class GetPendingIncidenceApprovals
    {
        public int CompanyId { get; set; }
        public string? UserId { get; set; }

        /// <summary>
        /// Filtro de estado: -1 = Todas, 0 = Pendientes (default), 1 = Aprobadas, 2 = Rechazadas.
        /// Coincide con AbsenceRequestStatus (Pending=0, Approved=1, Rejected=2).
        /// </summary>
        public int Status { get; set; } = 0;
    }
}
