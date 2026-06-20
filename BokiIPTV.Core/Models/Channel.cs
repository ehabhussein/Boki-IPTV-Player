using System.Text.Json.Serialization;
namespace BokiIPTV.Core.Models;

public sealed class Channel
{
    [JsonPropertyName("num")] public int Num { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("stream_id")] public int StreamId { get; init; }
    [JsonPropertyName("stream_icon")] public string? StreamIcon { get; init; }
    [JsonPropertyName("epg_channel_id")] public string? EpgChannelId { get; init; }
    [JsonPropertyName("category_id")] public string? CategoryId { get; init; }
}
