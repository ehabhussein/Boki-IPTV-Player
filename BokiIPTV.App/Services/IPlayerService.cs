using LibVLCSharp.WPF;
namespace BokiIPTV.App.Services;

public interface IPlayerService
{
    void Attach(VideoView view);
    void Play(string url);
    void Stop();
    void TogglePause();
    void SetVolume(double v01);
}
