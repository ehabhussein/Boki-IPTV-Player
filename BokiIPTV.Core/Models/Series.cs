using System.Text.Json.Serialization;
namespace BokiIPTV.Core.Models;

public sealed class Series
{
    [JsonPropertyName("series_id")] public int SeriesId { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("cover")] public string? Cover { get; init; }
    [JsonPropertyName("category_id")] public string? CategoryId { get; init; }
    [JsonPropertyName("plot")] public string? Plot { get; init; }
}
