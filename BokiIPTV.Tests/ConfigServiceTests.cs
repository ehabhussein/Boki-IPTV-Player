using BokiIPTV.Core.Services;
using Xunit;

public class ConfigServiceTests
{
    [Fact]
    public void Save_then_Load_roundtrips()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var svc = new ConfigService(dir);
        svc.Save(new AppConfig { BaseUrl = "http://x:8080", Username = "u", Password = "p", Volume = 0.5 });
        var loaded = svc.Load();
        Assert.Equal("u", loaded.Username);
        Assert.Equal(0.5, loaded.Volume);
    }

    [Fact]
    public void Load_returns_defaults_when_missing()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var loaded = new ConfigService(dir).Load();
        Assert.Equal("", loaded.Username);
    }
}
