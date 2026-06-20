using System.Text.Json;
namespace BokiIPTV.Core.Services;

public sealed class CacheService : ICacheService
{
    private sealed class Envelope<T> { public DateTimeOffset WrittenAt { get; set; } public T? Payload { get; set; } }

    private readonly string _dir;
    public CacheService(string directory) { _dir = directory; Directory.CreateDirectory(directory); }
    private string PathFor(string key) => Path.Combine(_dir, $"{key}.json");

    public async Task SetAsync<T>(string key, T value)
    {
        var env = new Envelope<T> { WrittenAt = DateTimeOffset.UtcNow, Payload = value };
        await File.WriteAllTextAsync(PathFor(key), JsonSerializer.Serialize(env));
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var path = PathFor(key);
        if (!File.Exists(path)) return default;
        var env = JsonSerializer.Deserialize<Envelope<T>>(await File.ReadAllTextAsync(path));
        if (env is null) return default;
        return IsFresh(env.WrittenAt, DateTimeOffset.UtcNow) ? env.Payload : default;
    }

    public static readonly TimeSpan Ttl = TimeSpan.FromHours(3);

    // 3-hour freshness rule. A future writtenAt (negative age, from a clock
    // change or edited cache) is treated as stale so we refetch rather than
    // trust a bogus timestamp. Exactly 3 hours old counts as stale (boundary
    // is exclusive) so a scheduled 3h refresh always wins.
    public bool IsFresh(DateTimeOffset writtenAt, DateTimeOffset now)
    {
        var age = now - writtenAt;
        return age >= TimeSpan.Zero && age < Ttl;
    }
}
