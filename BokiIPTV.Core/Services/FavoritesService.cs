using System.Text.Json;
namespace BokiIPTV.Core.Services;

public sealed class FavoritesService : IFavoritesService
{
    private readonly string _path;
    private readonly Dictionary<string, FavoriteEntry> _map;

    public FavoritesService(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "favorites.json");
        var list = File.Exists(_path)
            ? JsonSerializer.Deserialize<List<FavoriteEntry>>(File.ReadAllText(_path)) ?? new()
            : new();
        _map = list.ToDictionary(e => e.Key);
    }

    public IReadOnlyCollection<FavoriteEntry> Entries => _map.Values;
    public bool IsFavorite(string itemKey) => _map.ContainsKey(itemKey);

    private void Persist() => File.WriteAllText(_path, JsonSerializer.Serialize(_map.Values.ToList()));

    // Add if absent, remove if present; persist immediately so favorites survive
    // a crash or kill. Returns the new state (true = now a favorite) for the star UI.
    public bool Toggle(FavoriteEntry entry)
    {
        bool nowFavorite;
        if (_map.ContainsKey(entry.Key)) { _map.Remove(entry.Key); nowFavorite = false; }
        else { _map[entry.Key] = entry; nowFavorite = true; }
        Persist();
        return nowFavorite;
    }
}
