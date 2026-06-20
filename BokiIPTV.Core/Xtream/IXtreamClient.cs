using BokiIPTV.Core.Models;
namespace BokiIPTV.Core.Xtream;

public interface IXtreamClient
{
    Task<UserInfo> AuthenticateAsync(CancellationToken ct);
    Task<IReadOnlyList<Category>> GetLiveCategoriesAsync(CancellationToken ct);
    Task<IReadOnlyList<Channel>> GetLiveStreamsAsync(string categoryId, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetVodCategoriesAsync(CancellationToken ct);
    Task<IReadOnlyList<Movie>> GetVodStreamsAsync(string categoryId, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetSeriesCategoriesAsync(CancellationToken ct);
    Task<IReadOnlyList<Series>> GetSeriesAsync(string categoryId, CancellationToken ct);
    Task<IReadOnlyList<EpgEntry>> GetShortEpgAsync(int streamId, CancellationToken ct);
}
