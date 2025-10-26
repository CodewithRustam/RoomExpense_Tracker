namespace AppExpenseTrackerApi
{
    public class CustomTimestampEnricher : ILogEventEnricher
    {
        public const string TimestampPropertyName = "Timestamp";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var timestamp = DateTimeProvider.NowIST;
            var timestampProperty = new LogEventProperty(TimestampPropertyName, new ScalarValue(timestamp));
            logEvent.AddOrUpdateProperty(timestampProperty);
        }
    }
}
