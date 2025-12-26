namespace PrenominaApi.Models.Dto.Output
{
    public class ContractsOutput
    {
        public decimal Codigo {  get; set; }
        public decimal Company { get; set; }
        public int Folio { get; set; }
        public required string LastName { get; set; }
        public required string MLastName { get; set; }
        public required string Name { get; set; }
        public int? Ocupation { get; set; }
        public string? TenantName { get; set; }
        public string? Activity { get; set; }
        public int? Schedule { get; set; }
        public DateTime? SeniorityDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public int? Days { get; set; }
        public int ExpireInDays { get; set; } 
        public string? Observation { get; set; }
        public bool? ApplyRehired { get; set; }
        public int ContractDays { get; set; }
    }
}
