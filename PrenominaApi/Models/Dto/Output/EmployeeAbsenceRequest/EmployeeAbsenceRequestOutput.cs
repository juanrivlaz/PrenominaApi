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

        // Flujo de aprobación múltiple: si el código de incidencia tiene aprobadores configurados,
        // todos deben aprobar para que la solicitud quede aprobada.
        public bool RequiresApproval { get; set; }
        public int TotalApprovers { get; set; }
        public int ApprovedCount { get; set; }
        public bool AlreadyApprovedByMe { get; set; }
        // Verdadero si el usuario actual puede aprobar/rechazar (no requiere aprobadores, o
        // el usuario está configurado como aprobador del código de incidencia).
        public bool CanApprove { get; set; }
    }
}
