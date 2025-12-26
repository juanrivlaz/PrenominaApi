namespace PrenominaApi.Models.Dto.Output
{
    public class EmployeeAttendancesOutput : Employee
    {
        public required string Activity {  get; set; }
        public List<AttendanceOutput>? Attendances {  get; set; }
    }
}
