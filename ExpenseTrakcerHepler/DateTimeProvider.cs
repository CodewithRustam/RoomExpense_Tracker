using System.Globalization;

namespace ExpenseTrakcerHepler
{
    public static class DateTimeProvider
    {
        private static readonly TimeZoneInfo _indiaTimeZone = GetIndiaTimeZone();

        private static TimeZoneInfo GetIndiaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone("IST", TimeSpan.FromMinutes(330), "India Standard Time", "India Standard Time");
                }
            }
        }

        public static DateTime NowIST => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _indiaTimeZone);
        public static DateTime TodayIST => NowIST.Date;
        public static DateTime NowUtc => DateTime.UtcNow;
    }
    public class DateTimeParser
    {
        public static bool ParseMonthYear(string month, out DateTime parsedMonth)
        {
            return DateTime.TryParseExact(
                month,
                "yyyy-MM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedMonth
            );
        }
    }
}
