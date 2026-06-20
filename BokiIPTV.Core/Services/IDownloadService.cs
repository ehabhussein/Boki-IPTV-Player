namespace BokiIPTV.Core.Services;

public interface IDownloadService
{
    /// Streams a URL to a local file, reporting 0..1 progress. Throws on HTTP error / cancellation.
    Task DownloadAsync(string url, string filePath, IProgress<double>? progress, CancellationToken ct);
}
