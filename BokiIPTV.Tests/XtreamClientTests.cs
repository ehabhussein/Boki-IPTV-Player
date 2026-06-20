using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BokiIPTV.Core.Xtream;
using Xunit;

public class XtreamClientTests
{
    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        public string? LastUrl;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            LastUrl = r.RequestUri!.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    private static XtreamClient Make(string body, out StubHandler h)
    {
        h = new StubHandler(body);
        return new XtreamClient(new HttpClient(h),
            new XtreamCredentials("http://your-server.example:8080", "demo_user", "demo_pass"));
    }

    [Fact]
    public async Task Authenticate_parses_user_info()
    {
        var json = await File.ReadAllTextAsync("Fixtures/auth.json");
        var c = Make(json, out var h);
        var info = await c.AuthenticateAsync(CancellationToken.None);
        Assert.True(info.IsActive);
        Assert.Equal("1", info.MaxConnections);
        Assert.Contains("player_api.php?username=demo_user&password=demo_pass", h.LastUrl);
    }

    [Fact]
    public async Task LiveCategories_parses_list_and_sets_action()
    {
        var json = await File.ReadAllTextAsync("Fixtures/live_categories.json");
        var c = Make(json, out var h);
        var cats = await c.GetLiveCategoriesAsync(CancellationToken.None);
        Assert.Equal(2, cats.Count);
        Assert.Contains("action=get_live_categories", h.LastUrl);
    }
}
