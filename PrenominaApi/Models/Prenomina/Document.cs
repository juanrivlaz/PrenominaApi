using Microsoft.EntityFrameworkCore;
using PrenominaApi.Models.Prenomina.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("document")]
    [Index(nameof(Name), IsUnique = true)]
    public class Document
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("name")]
        public required string Name { get; set; }
        [Column("path")]
        public string? Path { get; set; }
        [Column("content")]
        public string? Content { get; set; }
        [Column("module")]
        public DocumentModule Module { get; set; } = DocumentModule.Generic;
        [Column("key_params")]
        public required IEnumerable<string> KeyParams { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
    }
}
