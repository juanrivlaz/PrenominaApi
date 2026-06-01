namespace PrenominaApi.Models.Dto.Input.Attendance
{
    /// <summary>
    /// Rechaza una incidencia pendiente de aprobación. Cualquier aprobador configurado puede
    /// rechazarla; al rechazarse no se reflejará en la prenómina.
    /// </summary>
    public class RejectIncidence
    {
        public required Guid AssistanceIncidentId { get; set; }
        public string? Comment { get; set; }
        public int CompanyId { get; set; }
        public string? UserId { get; set; }
    }
}
