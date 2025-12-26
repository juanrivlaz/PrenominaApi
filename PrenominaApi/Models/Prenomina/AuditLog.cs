using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("audit_log")]
    public class AuditLog
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("section_code")]
        public required string SectionCode { get; set; }
        [Column("record_id")]
        public required string RecordId { get; set; }
        [Column("description")]
        public required string Description { get; set; }
        [Column("old_value")]
        public required string OldValue { get; set; }
        [Column("new_value")]
        public required string NewValue { get; set; }
        [Column("by_user_id")]
        public required Guid ByUserId {  get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public virtual User? User { get; set; }
    }
}
