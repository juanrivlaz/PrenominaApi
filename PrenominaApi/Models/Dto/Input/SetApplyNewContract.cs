namespace PrenominaApi.Models.Dto.Input
{
    public class SetApplyNewContract
    {
        public int Codigo { get; set; }
        public decimal Company { get; set; }
        public int Folio { get; set; }
        public bool GenerateContract { get; set; }
        public int ContractDays { get; set; }
        public string? Observation { get; set; }
    }
}
