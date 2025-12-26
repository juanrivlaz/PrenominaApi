using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Index(nameof(Code), IsUnique = true)]
    [Table("section")]
    public class Section
    {
        [Key]
        [Column("code")]
        public required string Code { get; set; }
        [Column("name")]
        public required string Name { get; set; }
        [Column("description")]
        public required string Description { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        public IEnumerable<SectionRol>? Roles { get; set; }
    }
}
