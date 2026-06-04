namespace PrenominaApi.Models.Dto.Output.Attendance
{
    /// <summary>
    /// Pending incidence shown in the approvals inbox.
    /// </summary>
    public class PendingIncidenceApprovalOutput
    {
        public Guid Id { get; set; }
        // Permit group identifier (same value for incidences registered together from the
        // permits menu across multiple days). Null when it is an individual incidence.
        public Guid? RequestGroupId { get; set; }
        public int EmployeeCode { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string IncidentCode { get; set; } = string.Empty;
        public string IncidentDescription { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        // Approval progress: how many approvers are required and how many already approved.
        public int TotalApprovers { get; set; }
        public int ApprovedCount { get; set; }
        public bool AlreadyApprovedByMe { get; set; }
        // Current incidence status (to distinguish approved/rejected/pending when filtering).
        public bool Approved { get; set; }
        public bool Rejected { get; set; }
    }
}
