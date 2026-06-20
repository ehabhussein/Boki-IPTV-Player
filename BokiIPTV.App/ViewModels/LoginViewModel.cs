using System.Net.Http;
using BokiIPTV.Core.Services;
using BokiIPTV.Core.Xtream;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace BokiIPTV.App.ViewModels;

public partial class LoginViewModel(IConfigService config) : ObservableObject
{
    [ObservableProperty] private string _baseUrl = "http://your-server.example:8080";
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string? _error;
    [ObservableProperty] private bool _busy;

    [RelayCommand]
    private async Task LoginAsync()
    {
        Busy = true; Error = null;
        try
        {
            var cred = new XtreamCredentials(BaseUrl.Trim(), Username.Trim(), Password);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var info = await new XtreamClient(http, cred).AuthenticateAsync(CancellationToken.None);
            if (!info.IsActive) { Error = "Login failed: account not active or wrong credentials."; return; }

            var cfg = config.Load();
            cfg.BaseUrl = BaseUrl.Trim(); cfg.Username = Username.Trim(); cfg.Password = Password;
            config.Save(cfg);

            var main = App.Services.GetRequiredService<MainWindow>();
            main.Show();
            foreach (var w in System.Windows.Application.Current.Windows)
                if (w is LoginView lv) { lv.Close(); break; }
        }
        catch (Exception ex) { Error = $"Connection error: {ex.Message}"; }
        finally { Busy = false; }
    }
}
