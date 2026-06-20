namespace BokiIPTV.Core.Services;

public interface IFavoritesService
{
    bool IsFavorite(string itemKey);
    /// Adds the entry if its Key is absent, removes it if present; returns the
    /// new state (true = now a favorite). Persists immediately.
    bool Toggle(FavoriteEntry entry);
    IReadOnlyCollection<FavoriteEntry> Entries { get; }
}
