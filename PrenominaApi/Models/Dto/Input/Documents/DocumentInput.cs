using PrenominaApi.Models.Prenomina.Enums;
using System.ComponentModel.DataAnnotations;

namespace PrenominaApi.Models.Dto.Input.Documents
{
    public class DocumentInput
    {
        [Required]
        public required string Name { get; set; }
        public string? Path { get; set; }
        public string? Content { get; set; }
        public DocumentModule Module { get; set; } = DocumentModule.Generic;
        public List<string> KeyParams { get; set; } = new();
    }
}
