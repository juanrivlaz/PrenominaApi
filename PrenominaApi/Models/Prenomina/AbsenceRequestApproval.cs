using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models.Prenomina.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    /// <summary>
    /// Nivel de firma materializado (congelado) para una solicitud de ausencia concreta.
    /// Se crea al registrar la solicitud copiando la cadena del código, de modo que los
    /// cambios posteriores a la configuración no afecten las solicitudes en trámite.
    /// </summary>
    [Index(nameof(AbsenceRequestId))]
    [Table("absence_request_approval")]
    public class AbsenceRequestApproval
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Tipo de solicitud dueña de esta cadena (permiso o pago de horas extras).</summary>
        [Column("request_type")]
        public ApprovalRequestType RequestType { get; set; } = ApprovalRequestType.AbsenceRequest;

        /// <summary>
        /// Id de la solicitud dueña. Para permisos = EmployeeAbsenceRequests.Id; para pago de
        /// horas extras = OvertimePaymentRequest.Id. (La columna conserva el nombre histórico.)
        /// </summary>
        [Column("absence_request_id")]
        public required Guid AbsenceRequestId { get; set; }

        [Column("step_order")]
        public required int StepOrder { get; set; }

        [Column("role_id")]
        public required Guid RoleId { get; set; }

        [Column("scope")]
        public ApprovalScope Scope { get; set; } = ApprovalScope.Company;

        [Column("mode")]
        public ApprovalStepMode Mode { get; set; } = ApprovalStepMode.AnyOne;

        [Column("is_optional")]
        public bool IsOptional { get; set; } = false;

        [Column("status")]
        public ApprovalInstanceStatus Status { get; set; } = ApprovalInstanceStatus.Pending;

        /// <summary>
        /// Snapshot de los usuarios candidatos a firmar este nivel (CSV de GUIDs),
        /// resueltos al momento de crear la solicitud.
        /// </summary>
        [Column("resolved_candidate_user_ids")]
        public string? ResolvedCandidateUserIds { get; set; }

        /// <summary>
        /// Candidatos que ya firmaron este nivel (CSV de GUIDs). Relevante para el modo All,
        /// donde el nivel se aprueba sólo cuando todos los candidatos han firmado.
        /// </summary>
        [Column("signed_user_ids")]
        public string? SignedUserIds { get; set; }

        [Column("approved_by_user_id")]
        public Guid? ApprovedByUserId { get; set; }

        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [Column("comment")]
        [MaxLength(500)]
        public string? Comment { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
    }
}
