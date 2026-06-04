namespace PrenominaApi.Models.Dto.Input.Attendance
{
    /// <summary>
    /// Rechaza TODAS las incidencias de un grupo de permiso (mismo RequestGroupId),
    /// registradas juntas desde el menú de permisos.
    /// </summary>
    public class RejectIncidenceGroup
    {
        public required Guid RequestGroupId { get; set; }
        public string? Comment { get; set; }
        public int CompanyId { get; set; }
        public string? UserId { get; set; }
    }
}
