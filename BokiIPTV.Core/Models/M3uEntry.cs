namespace BokiIPTV.Core.Models;

/// One channel/stream parsed from an extended M3U (#EXTINF) playlist.
public sealed class M3uEntry
{
    public string Name { get; init; } = "";
    public string? Logo { get; init; }
    public string Group { get; init; } = "Uncategorized";
    public string Url { get; init; } = "";
}
