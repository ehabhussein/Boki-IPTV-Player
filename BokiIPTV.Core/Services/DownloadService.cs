namespace BokiIPTV.Core.Services;

public sealed class DownloadService(HttpClient http) : IDownloadService
{
    public async Task DownloadAsync(string url, string filePath, IProgress<double>? progress, CancellationToken ct)
    {
        using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength ?? -1L;

        await using var src = await resp.Content.ReadAsStreamAsync(ct);
        await using var dst = File.Create(filePath);

        var buffer = new byte[81920];
        long readTotal = 0;
        int n;
        while ((n = await src.ReadAsync(buffer, ct)) > 0)
        {
            await dst.WriteAsync(buffer.AsMemory(0, n), ct);
            readTotal += n;
            if (total > 0) progress?.Report(Math.Min(1.0, (double)readTotal / total));
        }
        progress?.Report(1.0);
    }
}
