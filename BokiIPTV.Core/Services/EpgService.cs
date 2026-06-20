using BokiIPTV.Core.Models;
using BokiIPTV.Core.Xtream;
namespace BokiIPTV.Core.Services;

public sealed class EpgService(IXtreamClient client, Func<DateTimeOffset> clock) : IEpgService
{
    public EpgService(IXtreamClient client) : this(client, () => DateTimeOffset.UtcNow) { }

    private async Task<IReadOnlyList<EpgEntry>> SafeFetch(Channel ch)
    {
        if (string.IsNullOrEmpty(ch.EpgChannelId)) return [];
        try { return await client.GetShortEpgAsync(ch.StreamId, CancellationToken.None); }
        catch { return []; }
    }

    public async Task<EpgEntry?> GetNowAsync(Channel channel)
    {
        var now = clock();
        foreach (var e in await SafeFetch(channel))
            if (e.Start <= now && now < e.End) return e;
        return null;
    }

    public async Task<IReadOnlyList<EpgEntry>> GetUpcomingAsync(Channel channel)
    {
        var now = clock();
        return (await SafeFetch(channel)).Where(e => e.Start > now).OrderBy(e => e.Start).ToList();
    }
}
