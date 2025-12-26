using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(Code), IsUnique = true)]
    [Table("key_value")]
    public class KeyValue
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        [Column("code")]
        public required string Code { get; set; }
        [Required]
        [Column("label")]
        public required string Label { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        [NotMapped]
        public virtual ICollection<ColumnIncidentOutputFile>? ColumnIncidentOutputFiles { get; set; }
    }
}
