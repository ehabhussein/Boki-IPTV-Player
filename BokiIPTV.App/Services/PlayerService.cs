using BokiIPTV.Core.Services;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

namespace BokiIPTV.App.Services;

public sealed class PlayerService : IPlayerService, IDisposable
{
    private readonly LibVLC _libvlc;
    private readonly MediaPlayer _player;
    private readonly PlayerGuard _guard = new();

    public PlayerService()
    {
        LibVLCSharp.Shared.Core.Initialize();   // loads native libvlc (qualified to avoid BokiIPTV.Core clash)
        _libvlc = new LibVLC();
        _player = new MediaPlayer(_libvlc);
        _player.Stopped += (_, _) => _guard.MarkStopped();
    }

    public void Attach(VideoView view) => view.MediaPlayer = _player;

    public void Stop() => _player.Stop();
    public void TogglePause() => _player.Pause();
    public void SetVolume(double v01) => _player.Volume = (int)Math.Clamp(v01 * 100, 0, 100);

    // Honors the account's max_connections:1: the guard stops the active stream
    // before we open the next one, so the server never sees two simultaneous
    // connections (which it would reject). network-caching smooths HLS/TS startup
    // at the cost of a little zap latency.
    public void Play(string url)
    {
        var target = _guard.BeginPlay(url, () => _player.Stop());
        using var media = new Media(_libvlc, new Uri(target));
        media.AddOption(":network-caching=1000");
        _player.Play(media);
    }

    public void Dispose() { _player.Dispose(); _libvlc.Dispose(); }
}
