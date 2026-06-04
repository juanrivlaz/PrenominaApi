using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Output.EmployeeAbsenceRequest
{
    public class EmployeeAbsenceRequestOutput
    {
        public required Guid Id { get; set; }
        public required string EmployeeName { get; set; }
        public required int EmployeeCode { get; set; }
        public required string EmployeeActivity { get; set; }
        public required string IncidentCode { get; set; }
        public required string IncidentDescription { get; set; }
        public required DateOnly StartDate { get; set; }
        public required DateOnly EndDate { get; set; }
        public string? Notes { get; set; } = null;
        public AbsenceRequestStatus Status { get; set; }
        public required DateTime CreatedAt { get; set; }

        // Multi-approval flow: if the incidence code has approvers configured,
        // all of them must approve for the request to be approved.
        public bool RequiresApproval { get; set; }
        public int TotalApprovers { get; set; }
        public int ApprovedCount { get; set; }
        public bool AlreadyApprovedByMe { get; set; }
        // True if the current user can approve/reject (no approvers required, or the user is
        // configured as an approver of the incidence code).
        public bool CanApprove { get; set; }
    }
}
