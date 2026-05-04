namespace PrenominaApi.Models.Dto.Output.EmployeeAbsenceRequest
{
    public class AbsenceRequestPdf
    {
        public required byte[] Bytes { get; set; }
        public required string FileName { get; set; }
    }
}
