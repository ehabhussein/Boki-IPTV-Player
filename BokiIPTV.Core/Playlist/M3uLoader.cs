using BokiIPTV.Core.Models;

namespace BokiIPTV.Core.Playlist;

public static class M3uLoader
{
    /// Loads a playlist from an http(s) URL or a local .m3u/.m3u8 file path.
    public static async Task<IReadOnlyList<M3uEntry>> LoadAsync(string source, HttpClient http, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(source)) return [];
        var content = source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                   || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? await http.GetStringAsync(source, ct)
            : await File.ReadAllTextAsync(source, ct);
        return M3uParser.Parse(content);
    }
}
