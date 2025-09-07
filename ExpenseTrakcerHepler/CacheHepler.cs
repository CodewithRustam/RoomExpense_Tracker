using Microsoft.Extensions.Caching.Memory;

namespace ExpenseTrakcerHepler
{
    public static class CacheHepler
    {
        public static string GetCacheKey(int roomId, DateTime monthDate)
        {
            try
            {
                return $"Room_{roomId}_Month_{monthDate:yyyyMM}";
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
