using System.Text.Json;
namespace BokiIPTV.Core.Services;

public sealed class ResumeService : IResumeService
{
    private readonly string _path;
    private readonly Dictionary<string, long> _positions;

    public ResumeService(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "resume.json");
        _positions = File.Exists(_path)
            ? JsonSerializer.Deserialize<Dictionary<string, long>>(File.ReadAllText(_path)) ?? new()
            : new();
    }

    public long GetMs(string key) => _positions.TryGetValue(key, out var ms) ? ms : 0;

    public void Save(string key, long positionMs, long durationMs)
    {
        // Ignore the first few seconds (not worth resuming) and treat the last 5% /
        // last 2 minutes as "finished" so it restarts from the top next time.
        bool finished = durationMs > 0 && positionMs >= durationMs - Math.Min(120_000, durationMs / 20);
        if (positionMs < 10_000 || finished)
        {
            if (_positions.Remove(key)) Persist();
            return;
        }
        _positions[key] = positionMs;
        Persist();
    }

    private void Persist() => File.WriteAllText(_path, JsonSerializer.Serialize(_positions));
}
