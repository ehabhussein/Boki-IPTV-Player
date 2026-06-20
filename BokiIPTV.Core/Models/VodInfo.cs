using System.Text.Json.Serialization;
namespace BokiIPTV.Core.Models;

public sealed class VodInfo
{
    [JsonPropertyName("info")] public VodMeta? Info { get; init; }
    [JsonPropertyName("movie_data")] public MovieData? MovieData { get; init; }
}

public sealed class VodMeta
{
    [JsonPropertyName("movie_image")] public string? MovieImage { get; init; }
    [JsonPropertyName("plot")] public string? Plot { get; init; }
    [JsonPropertyName("genre")] public string? Genre { get; init; }
    [JsonPropertyName("cast")] public string? Cast { get; init; }
    [JsonPropertyName("director")] public string? Director { get; init; }
    [JsonPropertyName("rating")] public string? Rating { get; init; }
    [JsonPropertyName("duration")] public string? Duration { get; init; }
}

public sealed class MovieData
{
    [JsonPropertyName("stream_id")] public int StreamId { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("container_extension")] public string? ContainerExtension { get; init; }
}
