namespace PrenominaApi.Models.Dto.Input.Attendance
{
    /// <summary>
    /// Rejects ALL incidences of a permit group (same RequestGroupId),
    /// registered together from the permits menu.
    /// </summary>
    public class RejectIncidenceGroup
    {
        public required Guid RequestGroupId { get; set; }
        public string? Comment { get; set; }
        public int CompanyId { get; set; }
        public string? UserId { get; set; }
    }
}
