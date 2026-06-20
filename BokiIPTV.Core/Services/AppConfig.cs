namespace BokiIPTV.Core.Services;

public sealed class AppConfig
{
    public string BaseUrl { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public double Volume { get; set; } = 1.0;
    public string? LastSection { get; set; }
    public string? M3uSource { get; set; }   // saved M3U playlist URL or file path
}
