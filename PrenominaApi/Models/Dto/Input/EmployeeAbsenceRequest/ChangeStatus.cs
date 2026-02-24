using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Input.EmployeeAbsenceRequest
{
    public class ChangeStatus
    {
        public string? Id { get; set; }
        public required AbsenceRequestStatus Status { get; set; }
    }
}
