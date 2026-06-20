using System.Text.Json;
namespace BokiIPTV.Core.Services;

public sealed class WatchHistoryService : IWatchHistoryService
{
    private const int MaxEntries = 100;
    private readonly string _path;
    private readonly List<FavoriteEntry> _items;   // most-recent-first

    public WatchHistoryService(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "history.json");
        _items = File.Exists(_path)
            ? JsonSerializer.Deserialize<List<FavoriteEntry>>(File.ReadAllText(_path)) ?? new()
            : new();
    }

    public IReadOnlyList<FavoriteEntry> Recent => _items;

    public void Record(FavoriteEntry entry)
    {
        _items.RemoveAll(e => e.Key == entry.Key);          // de-dupe: keep only the latest play
        _items.Insert(0, entry);
        if (_items.Count > MaxEntries) _items.RemoveRange(MaxEntries, _items.Count - MaxEntries);
        File.WriteAllText(_path, JsonSerializer.Serialize(_items));
    }
}
