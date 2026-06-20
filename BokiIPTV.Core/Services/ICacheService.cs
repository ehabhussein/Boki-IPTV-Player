namespace BokiIPTV.Core.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    bool IsFresh(DateTimeOffset writtenAt, DateTimeOffset now);
}
