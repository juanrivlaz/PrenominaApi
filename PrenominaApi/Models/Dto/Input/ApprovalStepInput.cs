using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Input
{
    /// <summary>
    /// Un nivel de la cadena de firmas que se configura para un código de incidencia.
    /// </summary>
    public class ApprovalStepInput
    {
        public int StepOrder { get; set; }
        public Guid RoleId { get; set; }
        public ApprovalScope Scope { get; set; } = ApprovalScope.Company;
        public ApprovalStepMode Mode { get; set; } = ApprovalStepMode.AnyOne;
        public bool IsOptional { get; set; } = false;
    }
}
