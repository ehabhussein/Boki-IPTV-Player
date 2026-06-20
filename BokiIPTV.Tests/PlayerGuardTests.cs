using BokiIPTV.Core.Services;
using Xunit;

public class PlayerGuardTests
{
    [Fact]
    public void Second_play_stops_the_first()
    {
        var guard = new PlayerGuard();
        int stops = 0;
        guard.BeginPlay("url-A", () => stops++);
        Assert.Equal(0, stops);                  // nothing was playing
        guard.BeginPlay("url-B", () => stops++); // must stop A first
        Assert.Equal(1, stops);
    }

    [Fact]
    public void Replaying_same_url_still_stops_first_to_respect_single_connection()
    {
        var guard = new PlayerGuard();
        int stops = 0;
        guard.BeginPlay("url-A", () => stops++);
        guard.BeginPlay("url-A", () => stops++);
        Assert.Equal(1, stops);
    }
}
