using System.Globalization;

namespace ExpenseTrakcerHepler
{
    public static class DateTimeProvider
    {
        private static readonly TimeZoneInfo _indiaTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        public static DateTime NowIST => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _indiaTimeZone);
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
