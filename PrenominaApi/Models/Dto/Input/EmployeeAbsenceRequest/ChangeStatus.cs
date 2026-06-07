using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Input.EmployeeAbsenceRequest
{
    public class ChangeStatus
    {
        public string? Id { get; set; }
        public required AbsenceRequestStatus Status { get; set; }
        /// <summary>Comentario opcional, usado principalmente al rechazar un nivel.</summary>
        public string? Comment { get; set; }
    }
}
