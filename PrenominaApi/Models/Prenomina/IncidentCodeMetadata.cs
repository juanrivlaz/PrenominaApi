using PrenominaApi.Models.Prenomina.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrenominaApi.Models.Prenomina
{
    [Table("incident_code_metadata")]
    public class IncidentCodeMetadata
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Column("amount", TypeName = "decimal(5, 2)")]
        public Decimal Amount { get; set; }
        [Column("math_operation")]
        public MathOperation MathOperation { get; set; }
        [Column("column_for_operation")]
        public ColumnForOperation ColumnForOperation { get; set; }
        [Column("custom_value", TypeName = "decimal(5, 2)")]
        public Decimal? CustomValue { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;
        public IncidentCode? IncidentCode { get; set; }
    }
}
