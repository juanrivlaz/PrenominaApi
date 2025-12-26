namespace PrenominaApi.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime ToSpecificTimeZone(this DateTime dateTime, TimeZoneInfo timeZone)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZone);
        }
    }
}
