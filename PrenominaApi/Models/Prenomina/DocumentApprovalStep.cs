using Microsoft.EntityFrameworkCore;
using PrenominaApi.Attributes;
using PrenominaApi.Models.Prenomina.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    /// <summary>
    /// Un nivel (paso) de la cadena de firmas configurada en un documento/contrato.
    /// La cadena ordenada de estos pasos describe quién firma y en qué orden. El documento
    /// se asigna a un código de incidencia, y desde ahí se toma la cadena al registrar el permiso.
    /// </summary>
    [Auditable("Nivel de aprobación", SectionCode.Contract, IdentifierProperties = new[] { "DocumentId" })]
    [Index(nameof(DocumentId))]
    [Table("document_approval_step")]
    public class DocumentApprovalStep
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Documento/contrato dueño de la cadena.</summary>
        [Column("document_id")]
        public required Guid DocumentId { get; set; }

        /// <summary>Orden de firma (1 = primero en firmar).</summary>
        [Column("step_order")]
        public required int StepOrder { get; set; }

        /// <summary>Rol que firma este nivel (Jefe de departamento, RH, etc.).</summary>
        [Column("role_id")]
        public required Guid RoleId { get; set; }

        /// <summary>Cómo se resuelve a la persona real respecto al empleado.</summary>
        [Column("scope")]
        public ApprovalScope Scope { get; set; } = ApprovalScope.Company;

        /// <summary>Cuántos firmantes del nivel se requieren.</summary>
        [Column("mode")]
        public ApprovalStepMode Mode { get; set; } = ApprovalStepMode.AnyOne;

        /// <summary>Si es opcional, se omite cuando no hay candidatos resolubles.</summary>
        [Column("is_optional")]
        public bool IsOptional { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
    }
}
