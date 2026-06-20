using System.Text.Json.Serialization;
namespace BokiIPTV.Core.Models;

public sealed class Episode
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("season")] public int Season { get; init; }
    [JsonPropertyName("episode_num")] public int EpisodeNum { get; init; }
    [JsonPropertyName("container_extension")] public string? ContainerExtension { get; init; }

    // Convenience label for list display (e.g. "S01E03 - Title"); not from the API.
    [JsonIgnore] public string Display => $"S{Season:00}E{EpisodeNum:00} - {Title}";
}
