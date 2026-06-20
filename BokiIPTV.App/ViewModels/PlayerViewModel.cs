using System.Windows.Threading;
using BokiIPTV.App.Services;
using BokiIPTV.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BokiIPTV.App.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    private readonly IPlayerService _player;
    private readonly IResumeService _resume;
    private readonly DispatcherTimer _timer;
    private bool _suppressSeek;   // true while we push the timer value into Position (so the setter doesn't seek)

    [ObservableProperty] private double _volume = 1.0;
    [ObservableProperty] private double _position;          // 0..1
    [ObservableProperty] private bool _isSeekable;
    [ObservableProperty] private string _timeText = "00:00 / 00:00";
    [ObservableProperty] private string? _nowPlaying;
    [ObservableProperty] private double _brightness = 1.0;
    [ObservableProperty] private double _contrast = 1.0;
    [ObservableProperty] private double _saturation = 1.0;
    [ObservableProperty] private double _speed = 1.0;

    public PlayerViewModel(IPlayerService player, IResumeService resume)
    {
        _player = player;
        _resume = resume;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    private void Tick()
    {
        try
        {
            _player.ApplyResumeIfReady();   // seek to saved position once playback is rolling

            IsSeekable = _player.IsSeekable;
            _suppressSeek = true;
            Position = _player.Position;
            _suppressSeek = false;
            TimeText = $"{Fmt(_player.TimeMs)} / {Fmt(_player.LengthMs)}";
            NowPlaying = _player.NowPlayingTitle;

            // Persist resume position — but not while a resume seek is still pending,
            // or we'd overwrite the saved spot with the ~0 start position.
            if (!_player.HasPendingResume && _player.CurrentKey is { } key && _player.TimeMs > 0)
                _resume.Save(key, _player.TimeMs, _player.LengthMs);
        }
        catch { /* a transient player state read can throw mid-transition; ignore */ }
    }

    private static string Fmt(long ms)
    {
        if (ms <= 0) return "00:00";
        var t = TimeSpan.FromMilliseconds(ms);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"mm\:ss");
    }

    partial void OnVolumeChanged(double value) => _player.SetVolume(value);
    partial void OnPositionChanged(double value) { if (!_suppressSeek) _player.Position = value; }
    partial void OnBrightnessChanged(double value) => _player.SetBrightness(value);
    partial void OnContrastChanged(double value) => _player.SetContrast(value);
    partial void OnSaturationChanged(double value) => _player.SetSaturation(value);
    partial void OnSpeedChanged(double value) => _player.SetSpeed(value);

    [RelayCommand] private void PlayPause() => _player.TogglePause();
    [RelayCommand] private void Stop() => _player.Stop();

    [RelayCommand]
    private void ResetAdjustments()
    {
        Brightness = 1.0; Contrast = 1.0; Saturation = 1.0; Speed = 1.0;
    }
}
