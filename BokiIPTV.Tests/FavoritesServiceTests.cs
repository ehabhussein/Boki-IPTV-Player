using BokiIPTV.Core.Services;
using Xunit;

public class FavoritesServiceTests
{
    private static FavoritesService New() =>
        new(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

    [Fact]
    public void Toggle_adds_then_removes_and_returns_state()
    {
        var f = New();
        Assert.True(f.Toggle("live:1"));   // now a favorite
        Assert.True(f.IsFavorite("live:1"));
        Assert.False(f.Toggle("live:1"));  // removed
        Assert.False(f.IsFavorite("live:1"));
    }

    [Fact]
    public void Favorites_persist_across_instances()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        new FavoritesService(dir).Toggle("vod:9");
        Assert.True(new FavoritesService(dir).IsFavorite("vod:9"));
    }
}
