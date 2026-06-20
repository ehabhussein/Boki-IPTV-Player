using BokiIPTV.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BokiIPTV.App.ViewModels;

public partial class PlayerViewModel(IPlayerService player) : ObservableObject
{
    [ObservableProperty] private double _volume = 1.0;
    partial void OnVolumeChanged(double value) => player.SetVolume(value);

    [RelayCommand] private void PlayPause() => player.TogglePause();
    [RelayCommand] private void Stop() => player.Stop();
}
