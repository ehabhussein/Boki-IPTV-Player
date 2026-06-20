using System.Text.Json;
namespace BokiIPTV.Core.Services;

public sealed class FavoritesService : IFavoritesService
{
    private readonly string _path;
    private readonly HashSet<string> _set;

    public FavoritesService(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "favorites.json");
        _set = File.Exists(_path)
            ? JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(_path)) ?? new()
            : new();
    }

    public IReadOnlyCollection<string> All => _set;
    public bool IsFavorite(string itemKey) => _set.Contains(itemKey);

    private void Persist() => File.WriteAllText(_path, JsonSerializer.Serialize(_set));

    // Add if absent, remove if present; persist immediately so favorites survive
    // a crash or kill. Returns the new state (true = now a favorite) for the star UI.
    public bool Toggle(string itemKey)
    {
        bool nowFavorite = _set.Add(itemKey);
        if (!nowFavorite) _set.Remove(itemKey);
        Persist();
        return nowFavorite;
    }
}
