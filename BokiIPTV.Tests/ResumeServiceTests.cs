using BokiIPTV.Core.Services;
using Xunit;

public class ResumeServiceTests
{
    private static ResumeService New() =>
        new(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

    [Fact]
    public void Saves_and_returns_mid_stream_position()
    {
        var r = New();
        r.Save("vod:1", 600_000, 3_600_000);   // 10 min into a 1h movie
        Assert.Equal(600_000, r.GetMs("vod:1"));
    }

    [Fact]
    public void Ignores_first_few_seconds()
    {
        var r = New();
        r.Save("vod:2", 4_000, 3_600_000);
        Assert.Equal(0, r.GetMs("vod:2"));
    }

    [Fact]
    public void Clears_when_finished_near_the_end()
    {
        var r = New();
        r.Save("vod:3", 600_000, 3_600_000);          // partway
        r.Save("vod:3", 3_595_000, 3_600_000);        // ~watched to end → finished
        Assert.Equal(0, r.GetMs("vod:3"));
    }
}
