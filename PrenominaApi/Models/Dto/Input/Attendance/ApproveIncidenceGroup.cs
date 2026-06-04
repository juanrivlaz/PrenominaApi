namespace PrenominaApi.Models.Dto.Input.Attendance
{
    /// <summary>
    /// Registra la aprobación del usuario actual sobre TODAS las incidencias de un grupo de
    /// permiso (mismo RequestGroupId), registradas juntas desde el menú de permisos.
    /// </summary>
    public class ApproveIncidenceGroup
    {
        public required Guid RequestGroupId { get; set; }
        public int CompanyId { get; set; }
        public string? UserId { get; set; }
    }
}
