using Microsoft.EntityFrameworkCore;
using PrenominaApi.Attributes;
using PrenominaApi.Models.Prenomina.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    /// <summary>
    /// Suplencia: durante un rango de fechas, <see cref="DelegateUserId"/> puede firmar
    /// en nombre de <see cref="UserId"/> (p. ej. vacaciones del responsable).
    /// </summary>
    [Auditable("Suplencia de aprobador", SectionCode.User, IdentifierProperties = new[] { "UserId" })]
    [Index(nameof(DelegateUserId))]
    [Index(nameof(UserId))]
    [Table("approver_delegation")]
    public class ApproverDelegation
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Usuario titular cuya firma se delega.</summary>
        [Column("user_id")]
        public required Guid UserId { get; set; }

        /// <summary>Usuario suplente que firma en nombre del titular.</summary>
        [Column("delegate_user_id")]
        public required Guid DelegateUserId { get; set; }

        [Column("from_date")]
        public required DateOnly FromDate { get; set; }

        /// <summary>Null = vigente indefinidamente.</summary>
        [Column("to_date")]
        public DateOnly? ToDate { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
    }
}
