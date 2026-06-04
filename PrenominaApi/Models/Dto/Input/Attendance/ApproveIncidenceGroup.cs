namespace PrenominaApi.Models.Dto.Input.Attendance
{
    /// <summary>
    /// Registers the current user's approval over ALL incidences of a permit group
    /// (same RequestGroupId), registered together from the permits menu.
    /// </summary>
    public class ApproveIncidenceGroup
    {
        public required Guid RequestGroupId { get; set; }
        public int CompanyId { get; set; }
        public string? UserId { get; set; }
    }
}
