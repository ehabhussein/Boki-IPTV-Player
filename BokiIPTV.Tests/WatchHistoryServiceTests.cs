using BokiIPTV.Core.Services;
using Xunit;

public class WatchHistoryServiceTests
{
    private static WatchHistoryService New() =>
        new(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

    private static FavoriteEntry E(string key, string title) =>
        new() { Key = key, Title = title, Kind = "vod", Url = "http://x/" + key };

    [Fact]
    public void Record_puts_newest_first_and_dedupes()
    {
        var h = New();
        h.Record(E("vod:1", "A"));
        h.Record(E("vod:2", "B"));
        h.Record(E("vod:1", "A again"));   // replays A → moves to front, no duplicate

        Assert.Equal(2, h.Recent.Count);
        Assert.Equal("vod:1", h.Recent[0].Key);
        Assert.Equal("vod:2", h.Recent[1].Key);
    }

    [Fact]
    public void History_persists_across_instances()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        new WatchHistoryService(dir).Record(E("vod:9", "Movie"));
        Assert.Equal("Movie", new WatchHistoryService(dir).Recent.Single().Title);
    }
}
