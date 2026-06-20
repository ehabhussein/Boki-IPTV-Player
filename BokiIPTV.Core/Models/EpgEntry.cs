namespace BokiIPTV.Core.Models;

public sealed class EpgEntry
{
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
}
