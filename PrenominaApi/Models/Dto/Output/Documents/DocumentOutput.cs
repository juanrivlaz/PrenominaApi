using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Output.Documents
{
    public class DocumentOutput
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Path { get; set; }
        public string? Content { get; set; }
        public DocumentModule Module { get; set; }
        public IEnumerable<string> KeyParams { get; set; } = new List<string>();
    }
}
