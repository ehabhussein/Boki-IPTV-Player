using System.Text.Json.Serialization;
namespace BokiIPTV.Core.Models;

public sealed class Movie
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("stream_id")] public int StreamId { get; init; }
    [JsonPropertyName("stream_icon")] public string? StreamIcon { get; init; }
    [JsonPropertyName("container_extension")] public string? ContainerExtension { get; init; }
    [JsonPropertyName("category_id")] public string? CategoryId { get; init; }
    [JsonPropertyName("rating")] public string? Rating { get; init; }
}
