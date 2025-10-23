using System.Globalization;

namespace ExpenseTrakcerHepler
{
    public class DateTimeProvider : IDateTimeProvider
    {
        private readonly TimeZoneInfo _indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        public DateTime NowIST => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _indiaTimeZone);
        public DateTime NowUtc => DateTime.UtcNow;
    }
    public interface IDateTimeProvider
    {
        DateTime NowIST { get; }
        DateTime NowUtc { get; }
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
