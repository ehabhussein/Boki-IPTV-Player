using System.Text.Json.Serialization;
namespace BokiIPTV.Core.Models;

public sealed class SeriesInfo
{
    [JsonPropertyName("info")] public SeriesMeta? Info { get; init; }
    [JsonPropertyName("episodes")] public Dictionary<string, List<Episode>>? Episodes { get; init; }
}

public sealed class SeriesMeta
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("cover")] public string? Cover { get; init; }
    [JsonPropertyName("plot")] public string? Plot { get; init; }
    [JsonPropertyName("genre")] public string? Genre { get; init; }
    [JsonPropertyName("cast")] public string? Cast { get; init; }
    [JsonPropertyName("director")] public string? Director { get; init; }
    [JsonPropertyName("releaseDate")] public string? ReleaseDate { get; init; }
    [JsonPropertyName("rating")] public string? Rating { get; init; }
}
