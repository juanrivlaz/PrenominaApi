using PrenominaApi.Models.Prenomina;

namespace PrenominaApi.Models.Dto.Input
{
    public class CreateIncidentOutputFile
    {
        public required string Name { get; set; }
        public required IEnumerable<ColumnIncidentOutputFile> Columns { get; set; }
    }
}
