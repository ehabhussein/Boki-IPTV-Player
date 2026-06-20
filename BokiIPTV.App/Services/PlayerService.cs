using BokiIPTV.Core.Services;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

namespace BokiIPTV.App.Services;

public sealed class PlayerService : IPlayerService, IDisposable
{
    private readonly LibVLC _libvlc;
    private readonly MediaPlayer _player;
    private readonly PlayerGuard _guard = new();

    private long _pendingResumeMs;

    public PlayerService()
    {
        LibVLCSharp.Shared.Core.Initialize();   // loads native libvlc (qualified to avoid BokiIPTV.Core clash)
        _libvlc = new LibVLC();
        _player = new MediaPlayer(_libvlc);
        _player.Stopped += (_, _) => _guard.MarkStopped();
        _player.Playing += (_, _) =>
        {
            if (_pendingResumeMs > 0) { _player.Time = _pendingResumeMs; _pendingResumeMs = 0; }
        };
    }

    public void Attach(VideoView view) => view.MediaPlayer = _player;

    public void Stop() => _player.Stop();
    public void TogglePause() => _player.Pause();
    public void SetVolume(double v01) => _player.Volume = (int)Math.Clamp(v01 * 100, 0, 100);

    public double Position
    {
        get => _player.Position;
        set { if (_player.IsSeekable) _player.Position = (float)Math.Clamp(value, 0, 1); }
    }
    public long TimeMs => _player.Time;
    public long LengthMs => _player.Length;
    public bool IsSeekable => _player.IsSeekable;

    // Honors the account's max_connections:1: the guard stops the active stream
    // before we open the next one, so the server never sees two simultaneous
    // connections (which it would reject). network-caching smooths HLS/TS startup
    // at the cost of a little zap latency.
    public string? NowPlayingTitle { get; private set; }
    public string? CurrentKey { get; private set; }

    public void Play(string url, string? title = null, string? resumeKey = null, long resumeMs = 0)
    {
        NowPlayingTitle = title;
        CurrentKey = resumeKey;
        _pendingResumeMs = resumeMs;
        var target = _guard.BeginPlay(url, () => _player.Stop());
        using var media = new Media(_libvlc, new Uri(target));
        media.AddOption(":network-caching=1000");
        _player.Play(media);
    }

    public void Dispose() { _player.Dispose(); _libvlc.Dispose(); }
}
