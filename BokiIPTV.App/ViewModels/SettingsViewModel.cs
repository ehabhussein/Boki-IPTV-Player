using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BokiIPTV.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly string _cacheDir;
    [ObservableProperty] private string _status = "";

    public SettingsViewModel() => _cacheDir = AppPaths.Cache;

    [RelayCommand]
    private void Refresh()
    {
        if (Directory.Exists(_cacheDir)) Directory.Delete(_cacheDir, true);
        Directory.CreateDirectory(_cacheDir);
        Status = "Cache cleared — reopen a section to reload.";
    }
}
