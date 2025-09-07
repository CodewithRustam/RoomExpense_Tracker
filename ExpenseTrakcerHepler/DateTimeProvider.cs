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

}
