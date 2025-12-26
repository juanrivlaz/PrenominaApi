namespace PrenominaApi.Services.Utilities
{
    public class DateService
    {
        public static List<DateOnly> GetListDate(DateOnly start, DateOnly end)
        {
            var dates = new List<DateOnly>();
            for (var date = start; date <= end; date = date.AddDays(1))
            {
                dates.Add(date);
            }

            return dates;
        }
    }
}
