namespace BokiIPTV.Core.Services;

public interface IWatchHistoryService
{
    /// Records a play. Moves an existing entry (same Key) to the front; caps the list.
    void Record(FavoriteEntry entry);
    /// Most-recent-first.
    IReadOnlyList<FavoriteEntry> Recent { get; }
}
