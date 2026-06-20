using System.Text.Json.Serialization;
namespace BokiIPTV.Core.Models;

public sealed class Episode
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("season")] public int Season { get; init; }
    [JsonPropertyName("container_extension")] public string? ContainerExtension { get; init; }
}
