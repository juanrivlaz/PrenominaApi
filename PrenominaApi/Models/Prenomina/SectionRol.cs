using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PrenominaApi.Models.Prenomina
{
    [Table("section_rol")]
    public class SectionRol
    {
        public required string SectionsCode {  get; set; }
        [NotMapped]
        public Section? Section { get; set; }
        public required Guid RolesId { get; set; }
        [NotMapped]
        public Role? Role { get; set; }
        [Column("permissions_json")]
        public string PermissionsJson { get; set; } = "{}";
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; } = null;

        [NotMapped]
        public Dictionary<string, bool> Permissions
        {
            get => string.IsNullOrEmpty(PermissionsJson) ? new Dictionary<string, bool>() : JsonSerializer.Deserialize<Dictionary<string, bool>>(PermissionsJson);

            set => PermissionsJson = JsonSerializer.Serialize(value);
        }
    }
}
