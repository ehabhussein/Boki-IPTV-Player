namespace BokiIPTV.Core.Services;

public interface IFavoritesService
{
    bool IsFavorite(string itemKey);
    bool Toggle(string itemKey);
    IReadOnlyCollection<string> All { get; }
}
