using BokiIPTV.Core.Models;
using BokiIPTV.Core.Services;
using BokiIPTV.Core.Xtream;
using Xunit;

public class EpgServiceTests
{
    private sealed class FakeClient(IReadOnlyList<EpgEntry> epg) : IXtreamClient
    {
        public Task<IReadOnlyList<EpgEntry>> GetShortEpgAsync(int s, CancellationToken ct) => Task.FromResult(epg);
        public Task<UserInfo> AuthenticateAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Category>> GetLiveCategoriesAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Channel>> GetLiveStreamsAsync(string c, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Channel>> GetAllLiveStreamsAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Category>> GetVodCategoriesAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Movie>> GetVodStreamsAsync(string c, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Movie>> GetAllVodStreamsAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Category>> GetSeriesCategoriesAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Series>> GetSeriesAsync(string c, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<Series>> GetAllSeriesAsync(CancellationToken ct) => throw new NotImplementedException();
        public Task<SeriesInfo> GetSeriesInfoAsync(int id, CancellationToken ct) => throw new NotImplementedException();
        public Task<VodInfo> GetVodInfoAsync(int id, CancellationToken ct) => throw new NotImplementedException();
    }

    private static readonly DateTimeOffset Now = DateTimeOffset.UnixEpoch.AddHours(10);

    [Fact]
    public async Task GetNow_returns_program_covering_now()
    {
        var epg = new[]
        {
            new EpgEntry { Title = "Past", Start = Now.AddHours(-2), End = Now.AddHours(-1) },
            new EpgEntry { Title = "Live", Start = Now.AddMinutes(-10), End = Now.AddMinutes(50) },
        };
        var svc = new EpgService(new FakeClient(epg), () => Now);
        var now = await svc.GetNowAsync(new Channel { StreamId = 1, EpgChannelId = "x" });
        Assert.Equal("Live", now!.Title);
    }

    [Fact]
    public async Task GetNow_returns_null_when_no_guide()
    {
        var svc = new EpgService(new FakeClient(Array.Empty<EpgEntry>()), () => Now);
        Assert.Null(await svc.GetNowAsync(new Channel { StreamId = 1, EpgChannelId = null }));
    }
}
