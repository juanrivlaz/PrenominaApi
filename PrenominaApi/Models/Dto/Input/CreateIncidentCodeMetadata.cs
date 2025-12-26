using PrenominaApi.Models.Prenomina.Enums;

namespace PrenominaApi.Models.Dto.Input
{
    public class CreateIncidentCodeMetadata
    {
        public ColumnForOperation ColumnForOperation { get; set; }
        public decimal? Amount { get; set; }
        public MathOperation MathOperation { get; set; }
    }
}
