using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Output.EmployeeAbsenceRequest
{
    public class EmployeeAbsenceRequestDetailOutput
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

        // Multi-approval progress.
        public bool RequiresApproval { get; set; }
        public int TotalApprovers { get; set; }
        public int ApprovedCount { get; set; }

        // Days covered by the request and the accumulated overtime consumed on each one.
        public int DaysCount { get; set; }
        public bool UsedOvertime { get; set; }
        public int TotalOvertimeMinutes { get; set; }
        public required string TotalOvertimeFormatted { get; set; }
        public required List<AbsenceRequestDayDetail> Days { get; set; }
    }

    public class AbsenceRequestDayDetail
    {
        public required DateOnly Date { get; set; }
        public int OvertimeMinutes { get; set; }
        public required string OvertimeFormatted { get; set; }
    }
}
