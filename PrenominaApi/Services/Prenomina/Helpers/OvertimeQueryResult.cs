namespace PrenominaApi.Services.Prenomina.Helpers
{
    internal class OvertimeQueryResult
    {
        public int EmployeeCode { get; set; }
        public TimeOnly CheckIn { get; set; }
        public TimeOnly? CheckOut { get; set; }
        public DateOnly Date { get; set; }
        public int TotalMinutesWorked { get; set; }
    }
}
