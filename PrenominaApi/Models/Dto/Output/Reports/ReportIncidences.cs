namespace PrenominaApi.Models.Dto.Output.Reports
{
    public class ReportIncidencesOutput
    {
        public required int Code { get; set; }
        public required string FullName { get; set; }
        public required string Department { get; set; }
        public required string JobPosition { get; set; }
        public required DateOnly Date { get; set; }
        public required string IncidenceCode { get; set; }
        public required string IncidenceDescription { get; set; }
        public required string UserFullName { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
