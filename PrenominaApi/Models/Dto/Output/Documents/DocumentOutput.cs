using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Output.Documents
{
    public class DocumentOutput
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Path { get; set; }
        public string? Content { get; set; }
        public DocumentModule Module { get; set; }
        public IEnumerable<string> KeyParams { get; set; } = new List<string>();
        public List<DocumentApprovalStepOutput> ApprovalSteps { get; set; } = new();
        /// <summary>Etiquetas de los roles que firman (cadena de firmas), en orden.</summary>
        public List<string> Signers { get; set; } = new();
    }

    public class DocumentApprovalStepOutput
    {
        public int StepOrder { get; set; }
        public Guid RoleId { get; set; }
        public int Scope { get; set; }
        public int Mode { get; set; }
        public bool IsOptional { get; set; }
    }
}
