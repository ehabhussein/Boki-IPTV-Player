using System.Text.Json.Serialization;
namespace BokiIPTV.Core.Models;

public sealed class Category
{
    [JsonPropertyName("category_id")] public string CategoryId { get; init; } = "";
    [JsonPropertyName("category_name")] public string CategoryName { get; init; } = "";
}
