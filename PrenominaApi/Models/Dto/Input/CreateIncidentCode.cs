using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Input
{
    public class CreateIncidentCode
    {
        public required string Code { get; set; }
        public required string ExternalCode { get; set; }
        public required string Label { get; set; }
        public string? Notes { get; set; }
        public bool RequiredApproval { get; set; }
        public bool WithOperation { get; set; }
        public bool IsAdditional { get; set; } = false;
        public bool RestrictedWithRoles { get; set; }
        public IncidentCodeApplyMode ApplyMode { get; set; }
        public CreateIncidentCodeMetadata? Metadata { get; set; }
        public virtual List<string>? IncidentApprovers { get; set; }
        public virtual List<string>? AllowedRoles { get; set; }
    }
}
