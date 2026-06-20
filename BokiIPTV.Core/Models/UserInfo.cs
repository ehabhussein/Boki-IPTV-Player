using System.Text.Json.Serialization;

namespace BokiIPTV.Core.Models;

public sealed class UserInfo
{
    [JsonPropertyName("auth")] public int Auth { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
    [JsonPropertyName("exp_date")] public string? ExpDate { get; init; }
    [JsonPropertyName("max_connections")] public string? MaxConnections { get; init; }
    [JsonPropertyName("allowed_output_formats")] public string[]? AllowedFormats { get; init; }
    public bool IsActive => Auth == 1 && string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);
}
