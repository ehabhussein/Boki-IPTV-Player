using BokiIPTV.Core.Models;
namespace BokiIPTV.Core.Services;

public interface IEpgService
{
    Task<EpgEntry?> GetNowAsync(Channel channel);
    Task<IReadOnlyList<EpgEntry>> GetUpcomingAsync(Channel channel);
}
