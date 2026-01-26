namespace PrenominaApi.Models.Dto.Input.Reports
{
    public class FilterByDates
    {
        public required DateTime Start { get; set; }
        public required DateTime End { get; set; }
    }
}
