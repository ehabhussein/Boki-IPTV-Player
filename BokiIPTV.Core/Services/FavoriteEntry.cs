namespace BokiIPTV.Core.Services;

/// A persisted favorite. Carries enough metadata to render and replay the item
/// without re-fetching the whole catalog. Kind is "live", "vod", or "series".
public sealed class FavoriteEntry
{
    public string Key { get; init; } = "";
    public string Title { get; init; } = "";
    public string Kind { get; init; } = "";
    public int StreamId { get; init; }
    public string? Ext { get; init; }
    public string? Icon { get; init; }

    // Lets the Favorites tab reuse the same item template that binds "Name".
    public string Name => Title;
}
