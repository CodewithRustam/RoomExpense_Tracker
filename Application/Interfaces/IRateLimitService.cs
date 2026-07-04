namespace Services.Interfaces
{
    public interface IRateLimitService
    {
        bool IsRateLimited(string key, int limit, TimeSpan window);
        bool ReachedDailyDeleteLimit(string userId);
        void IncrementDailyDeleteLimit(string userId);
    }
}