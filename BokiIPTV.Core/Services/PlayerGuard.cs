namespace BokiIPTV.Core.Services;

/// Enforces max_connections:1 — always stop the active stream before starting a new one.
public sealed class PlayerGuard
{
    private bool _playing;
    public string BeginPlay(string newUrl, Action stopCurrent)
    {
        if (_playing) stopCurrent();
        _playing = true;
        return newUrl;
    }
    public void MarkStopped() => _playing = false;
}
