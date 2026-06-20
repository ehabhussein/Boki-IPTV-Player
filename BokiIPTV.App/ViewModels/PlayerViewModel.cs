using System.Windows.Threading;
using BokiIPTV.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BokiIPTV.App.ViewModels;

public partial class PlayerViewModel : ObservableObject
{
    private readonly IPlayerService _player;
    private readonly DispatcherTimer _timer;
    private bool _suppressSeek;   // true while we push the timer value into Position (so the setter doesn't seek)

    [ObservableProperty] private double _volume = 1.0;
    [ObservableProperty] private double _position;          // 0..1
    [ObservableProperty] private bool _isSeekable;
    [ObservableProperty] private string _timeText = "00:00 / 00:00";

    public PlayerViewModel(IPlayerService player)
    {
        _player = player;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    private void Tick()
    {
        IsSeekable = _player.IsSeekable;
        _suppressSeek = true;
        Position = _player.Position;
        _suppressSeek = false;
        TimeText = $"{Fmt(_player.TimeMs)} / {Fmt(_player.LengthMs)}";
    }

    private static string Fmt(long ms)
    {
        if (ms <= 0) return "00:00";
        var t = TimeSpan.FromMilliseconds(ms);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"mm\:ss");
    }

    partial void OnVolumeChanged(double value) => _player.SetVolume(value);
    partial void OnPositionChanged(double value) { if (!_suppressSeek) _player.Position = value; }

    [RelayCommand] private void PlayPause() => _player.TogglePause();
    [RelayCommand] private void Stop() => _player.Stop();
}
