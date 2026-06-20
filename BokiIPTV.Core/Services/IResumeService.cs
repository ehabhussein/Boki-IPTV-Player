namespace BokiIPTV.Core.Services;

public interface IResumeService
{
    /// Saved resume position in ms for a stream key, or 0 if none / finished.
    long GetMs(string key);
    /// Persist progress. Treats near-the-end as finished (clears the entry).
    void Save(string key, long positionMs, long durationMs);
}
