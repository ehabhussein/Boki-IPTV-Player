using LibVLCSharp.WPF;
namespace BokiIPTV.App.Services;

public interface IPlayerService
{
    void Attach(VideoView view);
    void Play(string url, string? title = null, string? resumeKey = null, long resumeMs = 0);
    string? NowPlayingTitle { get; }
    string? CurrentKey { get; }
    bool HasPendingResume { get; }
    void ApplyResumeIfReady();
    void Stop();

    void SetBrightness(double v);   // 0..2  (1 = normal)
    void SetContrast(double v);     // 0..2  (1 = normal)
    void SetSaturation(double v);   // 0..3  (1 = normal)
    void SetSpeed(double v);        // 0.25..2 (1 = normal)
    void TogglePause();
    void SetVolume(double v01);

    /// 0..1 position within the stream. Settable to seek (VOD only).
    double Position { get; set; }
    long TimeMs { get; }
    long LengthMs { get; }
    bool IsSeekable { get; }
}
