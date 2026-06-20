using BokiIPTV.Core.Services;
using Xunit;

public class FavoritesServiceTests
{
    private static FavoritesService New() =>
        new(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

    private static FavoriteEntry Entry(string key) =>
        new() { Key = key, Title = key, Kind = "live", StreamId = 1 };

    [Fact]
    public void Toggle_adds_then_removes_and_returns_state()
    {
        var f = New();
        Assert.True(f.Toggle(Entry("live:1")));   // now a favorite
        Assert.True(f.IsFavorite("live:1"));
        Assert.False(f.Toggle(Entry("live:1")));  // removed
        Assert.False(f.IsFavorite("live:1"));
    }

    [Fact]
    public void Favorites_persist_across_instances()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        new FavoritesService(dir).Toggle(new FavoriteEntry { Key = "vod:9", Title = "Movie", Kind = "vod", StreamId = 9 });
        var reloaded = new FavoritesService(dir);
        Assert.True(reloaded.IsFavorite("vod:9"));
        Assert.Equal("Movie", reloaded.Entries.Single().Title);
    }
}
