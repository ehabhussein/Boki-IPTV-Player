using System.Windows;
using BokiIPTV.App.Services;
using BokiIPTV.App.ViewModels;
using BokiIPTV.Core.Services;
using BokiIPTV.Core.Xtream;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BokiIPTV.App;

public partial class App : Application
{
    private IHost _host = null!;
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfigService>(_ => new ConfigService(AppPaths.Root));
                services.AddSingleton<ICacheService>(_ => new CacheService(AppPaths.Cache));
                services.AddSingleton<IFavoritesService>(_ => new FavoritesService(AppPaths.Root));
                services.AddSingleton<IWatchHistoryService>(_ => new WatchHistoryService(AppPaths.Root));
                services.AddSingleton<IResumeService>(_ => new ResumeService(AppPaths.Root));

                services.AddSingleton(sp =>
                {
                    var cfg = sp.GetRequiredService<IConfigService>().Load();
                    return new XtreamCredentials(cfg.BaseUrl, cfg.Username, cfg.Password);
                });
                services.AddHttpClient<IXtreamClient, XtreamClient>();
                services.AddSingleton<IEpgService, EpgService>();

                services.AddSingleton<IPlayerService, PlayerService>();
                services.AddSingleton<MainViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
        Services = _host.Services;

        var cfg = Services.GetRequiredService<IConfigService>().Load();
        Window start = string.IsNullOrWhiteSpace(cfg.Username)
            ? new LoginView { DataContext = Services.GetRequiredService<LoginViewModel>() }
            : Services.GetRequiredService<MainWindow>();
        start.Show();
    }

    protected override void OnExit(ExitEventArgs e) { _host?.Dispose(); base.OnExit(e); }
}
