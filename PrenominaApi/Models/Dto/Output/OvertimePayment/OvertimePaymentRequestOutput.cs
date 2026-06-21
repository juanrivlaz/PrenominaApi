using PrenominaApi.Models.Dto.Output.EmployeeAbsenceRequest;
using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Output.OvertimePayment
{
    public class OvertimePaymentRequestOutput
    {
        public required Guid Id { get; set; }
        public required string EmployeeName { get; set; }
        public required int EmployeeCode { get; set; }
        public int TotalMinutes { get; set; }
        public required string TotalMinutesFormatted { get; set; }
        public AbsenceRequestStatus Status { get; set; }
        public required DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }

        public bool RequiresApproval { get; set; }
        public int TotalApprovers { get; set; }
        public int ApprovedCount { get; set; }
        public bool AlreadyApprovedByMe { get; set; }
        public bool CanApprove { get; set; }
    }

    public class OvertimePaymentRequestDetailOutput
    {
        public required Guid Id { get; set; }
        public required string EmployeeName { get; set; }
        public required int EmployeeCode { get; set; }
        public int TotalMinutes { get; set; }
        public required string TotalMinutesFormatted { get; set; }
        public AbsenceRequestStatus Status { get; set; }
        public required DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
        /// <summary>Fechas origen de las horas extras que cubre la papeleta (movimientos de pago directo).</summary>
        public List<DateTime> OvertimeDates { get; set; } = new();
        public List<AbsenceRequestApprovalStepOutput> ApprovalChain { get; set; } = new();
    }
}
