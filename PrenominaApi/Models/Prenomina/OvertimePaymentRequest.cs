using Microsoft.EntityFrameworkCore;
using PrenominaApi.Attributes;
using PrenominaApi.Models.Prenomina.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    /// <summary>
    /// Solicitud/papeleta de pago de horas extras. Se genera al mandar a pagar horas extra
    /// (individual o en lote → una papeleta para todas las horas pagadas del empleado) y pasa
    /// por una cadena de firmas. Si se rechaza, las horas se reintegran a pendientes.
    /// </summary>
    [Auditable("Solicitud de pago de horas extras", SectionCode.Overtime, IdentifierProperties = new[] { "EmployeeCode" })]
    [Index(nameof(EmployeeCode))]
    [Index(nameof(CompanyId))]
    [Table("overtime_payment_request")]
    public class OvertimePaymentRequest
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("employee_code")]
        public required int EmployeeCode { get; set; }

        [Column("company_id")]
        public required int CompanyId { get; set; }

        /// <summary>Total de minutos de horas extra que cubre esta papeleta.</summary>
        [Column("total_minutes")]
        public required int TotalMinutes { get; set; }

        /// <summary>Documento/contrato (módulo Pago de horas extras) usado para el formato/cadena.</summary>
        [Column("document_id")]
        public Guid? DocumentId { get; set; }

        [Column("status")]
        public AbsenceRequestStatus Status { get; set; } = AbsenceRequestStatus.Pending;

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_by_user_id")]
        public Guid? CreatedByUserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
    }
}
