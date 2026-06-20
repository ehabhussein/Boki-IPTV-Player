using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BokiIPTV.Core.Services;
using Xunit;

public class DownloadServiceTests
{
    private sealed class ByteHandler(byte[] body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });
    }

    [Fact]
    public async Task Downloads_full_content_and_reports_completion()
    {
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var svc = new DownloadService(new HttpClient(new ByteHandler(data)));
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        double last = 0;
        var progress = new Progress<double>(p => last = p);

        await svc.DownloadAsync("http://x/movie.mp4", path, progress, CancellationToken.None);

        Assert.Equal(data, await File.ReadAllBytesAsync(path));
        Assert.Equal(1.0, last, 3);
        File.Delete(path);
    }

    [Fact]
    public async Task Throws_on_http_error()
    {
        var handler = new ErrorHandler();
        var svc = new DownloadService(new HttpClient(handler));
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            svc.DownloadAsync("http://x/missing.mp4", path, null, CancellationToken.None));
    }

    private sealed class ErrorHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
