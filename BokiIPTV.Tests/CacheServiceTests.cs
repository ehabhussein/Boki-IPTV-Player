using BokiIPTV.Core.Services;
using Xunit;

public class CacheServiceTests
{
    private static CacheService New() =>
        new(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

    [Fact]
    public async Task Set_then_Get_returns_value_when_fresh()
    {
        var c = New();
        await c.SetAsync("k", new[] { 1, 2, 3 });
        Assert.Equal(new[] { 1, 2, 3 }, await c.GetAsync<int[]>("k"));
    }

    [Fact]
    public void IsFresh_true_within_3_hours_false_after()
    {
        var c = New();
        var t0 = DateTimeOffset.UnixEpoch;
        Assert.True(c.IsFresh(t0, t0.AddHours(2)));
        Assert.False(c.IsFresh(t0, t0.AddHours(4)));
    }

    [Fact]
    public async Task Get_returns_null_when_missing() => Assert.Null(await New().GetAsync<int[]>("nope"));
}
