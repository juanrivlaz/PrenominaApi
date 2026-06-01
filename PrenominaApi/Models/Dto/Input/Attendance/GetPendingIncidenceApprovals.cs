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
    }
}
