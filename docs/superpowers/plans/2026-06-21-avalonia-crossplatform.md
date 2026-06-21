# BokiIPTV Cross-Platform (Avalonia) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port BokiIPTV's UI from WPF to Avalonia 11 so it runs on Windows, macOS, and Linux, reusing `BokiIPTV.Core` unchanged (except a small credentials-provider addition), add an in-app Settings screen, and set up GitHub Actions CI/release for all three OSes.

**Architecture:** New `BokiIPTV.Avalonia` app (Avalonia 11, .NET 10) referencing the existing portable `BokiIPTV.Core`. ViewModels are ported from the WPF app and adjusted only at platform seams (timer, player surface, dialogs, converters). WPF (`BokiIPTV.App`) stays frozen as a fallback until Avalonia is verified, then removed.

**Tech Stack:** Avalonia 11, LibVLCSharp + LibVLCSharp.Avalonia, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting/DI/Http, xUnit, GitHub Actions.

## Global Constraints

- Target framework `net10.0` for the Avalonia app (cross-platform; not `-windows`).
- Avalonia version **11.x**; `LibVLCSharp.Avalonia` 3.x; `CommunityToolkit.Mvvm` 8.x.
- `Nullable` enabled, `ImplicitUsings` enabled in the Avalonia project.
- Native libVLC: Windows → `VideoLAN.LibVLC.Windows`; macOS → `VideoLAN.LibVLC.Mac` (both via RID-conditional `PackageReference`); Linux → system libVLC (documented, not bundled).
- `BokiIPTV.Core` stays portable — no `System.Windows`, no Avalonia, no UI types.
- Data locations unchanged: `%AppData%`/`$XDG_CONFIG_HOME`/`~/Library/Application Support` resolved via `Environment.SpecialFolder.ApplicationData` (already used by `AppPaths`; replicate in Avalonia).
- Feature parity with WPF v1.3.0.

## File Structure

```
BokiIPTV.Core/
  Services/ICredentialsProvider.cs        NEW — mutable current credentials
  Services/CredentialsProvider.cs         NEW
  Xtream/XtreamClient.cs                   MODIFY — read creds from provider
BokiIPTV.Avalonia/                          NEW project
  BokiIPTV.Avalonia.csproj
  Program.cs                               entry point + BuildAvaloniaApp
  App.axaml(.cs)                           DI host + startup window
  AppPaths.cs                              %AppData%/BokiIPTV paths (copied)
  Converters/{PosterConverter,BoolToVisibilityConverter,NotConverter}.cs
  Services/{IPlayerService,PlayerService}.cs   Avalonia VideoView + libVLC
  ViewModels/{MainViewModel,SectionViewModel,PlayerViewModel,
              SettingsViewModel,LoginViewModel}.cs   ported
  Views/{MainWindow,LoginWindow,AddPlaylistWindow,SettingsView}.axaml(.cs)
  Assets/icon.ico (or .png)
BokiIPTV.Tests/
  CredentialsProviderTests.cs              NEW
.github/workflows/ci.yml                    NEW
.github/workflows/release.yml               NEW
```

Reference source for the port (translate, don't reinvent): the existing WPF files
under `BokiIPTV.App/` — `MainWindow.xaml`, `Views/LoginView.xaml`,
`Views/AddPlaylistWindow.xaml`, `App.xaml(.cs)`, the three converters, the five
ViewModels, `Services/PlayerService.cs`.

### WPF → Avalonia substitution rules (apply throughout the port)

| WPF | Avalonia |
|---|---|
| `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` | `xmlns="https://github.com/avaloniaui"` |
| `clr-namespace:X;assembly=Y` | same syntax (Avalonia supports it) |
| `Visibility="Collapsed"/"Visible"` | `IsVisible="False"/"True"` (bool) |
| `BoolToVisibilityConverter` | bind `IsVisible` to a bool directly (drop the converter where possible) |
| `System.Windows.Threading.DispatcherTimer` | `Avalonia.Threading.DispatcherTimer` |
| `System.Windows.Data.IValueConverter` | `Avalonia.Data.Converters.IValueConverter` (signature returns `object?`, takes `Type`, `object?`, `CultureInfo`) |
| `BitmapImage` (PosterConverter) | `Avalonia.Media.Imaging.Bitmap` from a stream |
| `LibVLCSharp.WPF` `VideoView` | `LibVLCSharp.Avalonia` `VideoView` |
| `Microsoft.Win32.SaveFileDialog/OpenFileDialog` | `TopLevel.StorageProvider.SaveFilePickerAsync / OpenFilePickerAsync` |
| `Window` `Icon="x.ico"` | `Icon="/Assets/icon.ico"` via `WindowIcon` |
| `Style TargetType` + `Setter`/`Trigger` | Avalonia `Style Selector` + setters + `:pointerover`/`:checked` pseudo-classes |
| `EventSetter` `MouseDoubleClick` | handle `DoubleTapped` on the control |
| `PasswordBox` | `TextBox` with `PasswordChar="•"` |
| `x:Static local:Conv.Instance` | `{x:Static conv:Conv.Instance}` (same) |

---

## Task 1: Core — `ICredentialsProvider` + `XtreamClient` reads it (TDD)

**Files:**
- Create: `BokiIPTV.Core/Services/ICredentialsProvider.cs`, `BokiIPTV.Core/Services/CredentialsProvider.cs`
- Modify: `BokiIPTV.Core/Xtream/XtreamClient.cs`
- Test: `BokiIPTV.Tests/CredentialsProviderTests.cs`

**Interfaces:**
- Produces:
  - `interface ICredentialsProvider { XtreamCredentials Current { get; } void Update(XtreamCredentials c); }`
  - `sealed class CredentialsProvider : ICredentialsProvider` (ctor takes initial `XtreamCredentials`)
  - `XtreamClient` constructor changes to `XtreamClient(HttpClient http, ICredentialsProvider creds)`; every request reads `creds.Current`.
- Consumes: existing `XtreamCredentials` record.

- [ ] **Step 1: Write the failing test**

`BokiIPTV.Tests/CredentialsProviderTests.cs`:

```csharp
using System.Net;
using BokiIPTV.Core.Services;
using BokiIPTV.Core.Xtream;
using Xunit;

public class CredentialsProviderTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public string? LastUrl;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            LastUrl = r.RequestUri!.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = new StringContent("{\"user_info\":{\"auth\":1,\"status\":\"Active\"}}") });
        }
    }

    [Fact]
    public async Task Client_uses_updated_credentials()
    {
        var provider = new CredentialsProvider(new XtreamCredentials("http://a:8080", "u1", "p1"));
        var handler = new StubHandler();
        var client = new XtreamClient(new HttpClient(handler), provider);

        await client.AuthenticateAsync(CancellationToken.None);
        Assert.Contains("http://a:8080/player_api.php?username=u1&password=p1", handler.LastUrl);

        provider.Update(new XtreamCredentials("http://b:9090", "u2", "p2"));
        await client.AuthenticateAsync(CancellationToken.None);
        Assert.Contains("http://b:9090/player_api.php?username=u2&password=p2", handler.LastUrl);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter CredentialsProviderTests`
Expected: FAIL — `ICredentialsProvider`/`CredentialsProvider` not defined; `XtreamClient` ctor mismatch.

- [ ] **Step 3: Implement the provider**

`BokiIPTV.Core/Services/ICredentialsProvider.cs`:

```csharp
using BokiIPTV.Core.Xtream;
namespace BokiIPTV.Core.Services;

public interface ICredentialsProvider
{
    XtreamCredentials Current { get; }
    void Update(XtreamCredentials credentials);
}
```

`BokiIPTV.Core/Services/CredentialsProvider.cs`:

```csharp
using BokiIPTV.Core.Xtream;
namespace BokiIPTV.Core.Services;

public sealed class CredentialsProvider(XtreamCredentials initial) : ICredentialsProvider
{
    public XtreamCredentials Current { get; private set; } = initial;
    public void Update(XtreamCredentials credentials) => Current = credentials;
}
```

- [ ] **Step 4: Modify `XtreamClient` to read from the provider**

In `BokiIPTV.Core/Xtream/XtreamClient.cs`, change the primary constructor and the
`Url` builder to read current credentials:

```csharp
public sealed class XtreamClient(HttpClient http, ICredentialsProvider creds) : IXtreamClient
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private string Url(string? action, params (string k, string v)[] extra)
    {
        var c = creds.Current;
        var sb = new StringBuilder(c.BaseUrl.TrimEnd('/'))
            .Append("/player_api.php?username=").Append(c.Username)
            .Append("&password=").Append(c.Password);
        if (action is not null) sb.Append("&action=").Append(action);
        foreach (var (k, v) in extra) sb.Append('&').Append(k).Append('=').Append(v);
        return sb.ToString();
    }
    // ... rest of the class unchanged ...
}
```

Add `using BokiIPTV.Core.Services;` to the file.

- [ ] **Step 5: Fix the existing `XtreamClientTests` construction**

`XtreamClientTests` currently does `new XtreamClient(new HttpClient(h), new XtreamCredentials(...))`.
Change those two call sites to wrap the credentials:

```csharp
return new XtreamClient(new HttpClient(h),
    new CredentialsProvider(new XtreamCredentials("http://your-server.example:8080", "demo_user", "demo_pass")));
```

Add `using BokiIPTV.Core.Services;` to `XtreamClientTests.cs`.

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test`
Expected: PASS — all prior tests (27) plus the new one (28 total).

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat(core): ICredentialsProvider so XtreamClient creds can change at runtime"
```

---

## Task 2: Scaffold `BokiIPTV.Avalonia` project

**Files:**
- Create: `BokiIPTV.Avalonia/BokiIPTV.Avalonia.csproj`, `BokiIPTV.Avalonia/Program.cs`, `BokiIPTV.Avalonia/app.manifest` (optional Windows)
- Modify: `BokiIPTV.sln`

**Interfaces:**
- Produces: a buildable (empty-window) Avalonia app referencing Core.

- [ ] **Step 1: Create the project from the Avalonia template**

```bash
cd "D:/Repositories/BokiIPTV"
dotnet new install Avalonia.Templates
dotnet new avalonia.app -n BokiIPTV.Avalonia -o BokiIPTV.Avalonia
dotnet sln add BokiIPTV.Avalonia
dotnet add BokiIPTV.Avalonia reference BokiIPTV.Core
```

- [ ] **Step 2: Set the csproj properties and packages**

Edit `BokiIPTV.Avalonia/BokiIPTV.Avalonia.csproj` so the `PropertyGroup` has
`net10.0`, nullable, implicit usings, and `OutputType=WinExe`; and add packages
plus RID-conditional native libVLC:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.2.3" />
    <PackageReference Include="Avalonia.Desktop" Version="11.2.3" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.3" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.2.3" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.9" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="10.0.9" />
    <PackageReference Include="LibVLCSharp" Version="3.10.0" />
    <PackageReference Include="LibVLCSharp.Avalonia" Version="3.10.0" />
  </ItemGroup>

  <ItemGroup Condition="$([MSBuild]::IsOSPlatform('Windows'))">
    <PackageReference Include="VideoLAN.LibVLC.Windows" Version="3.0.21" />
  </ItemGroup>
  <ItemGroup Condition="$([MSBuild]::IsOSPlatform('OSX'))">
    <PackageReference Include="VideoLAN.LibVLC.Mac" Version="3.1.3.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\BokiIPTV.Core\BokiIPTV.Core.csproj" />
  </ItemGroup>
</Project>
```

> Note: the exact latest patch versions can be taken at install time; the floors above are known-good. The OS-conditional groups bundle native libVLC on Windows/macOS; Linux relies on the system package.

- [ ] **Step 3: Build to verify scaffold compiles**

Run: `dotnet build BokiIPTV.Avalonia`
Expected: `Build succeeded` (template's default window).

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "chore: scaffold BokiIPTV.Avalonia project"
```

---

## Task 3: Avalonia converters + AppPaths

**Files:**
- Create: `BokiIPTV.Avalonia/AppPaths.cs`, `BokiIPTV.Avalonia/Converters/PosterConverter.cs`, `Converters/NotConverter.cs`
- (BoolToVisibility is unnecessary in Avalonia — bind `IsVisible` to bools directly.)

**Interfaces:**
- Produces: `AppPaths.Root`/`AppPaths.Cache`; `PosterConverter.Instance` (string URL → `Bitmap?`); `NotConverter.Instance` (bool → !bool).

- [ ] **Step 1: AppPaths (copied from WPF)**

`BokiIPTV.Avalonia/AppPaths.cs`:

```csharp
using System.IO;
namespace BokiIPTV.Avalonia;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BokiIPTV");
    public static string Cache { get; } = Path.Combine(Root, "cache");
}
```

- [ ] **Step 2: PosterConverter (downloads to a Bitmap, async-safe, swallows errors)**

`BokiIPTV.Avalonia/Converters/PosterConverter.cs`:

```csharp
using System.Globalization;
using System.Net.Http;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace BokiIPTV.Avalonia.Converters;

// Loads a remote image URL into a Bitmap. Returns null on any failure so a broken
// poster never throws. Uses a small static cache to avoid re-downloading.
public sealed class PosterConverter : IValueConverter
{
    public static readonly PosterConverter Instance = new();
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly Dictionary<string, Bitmap?> Cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url)) return null;
        if (Cache.TryGetValue(url, out var cached)) return cached;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) { Cache[url] = null; return null; }
        try
        {
            using var stream = Http.GetStreamAsync(uri).GetAwaiter().GetResult();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            var bmp = new Bitmap(ms);
            Cache[url] = bmp;
            return bmp;
        }
        catch { Cache[url] = null; return null; }
    }

    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c) => null;
}
```

> Synchronous fetch inside a converter is acceptable for posters in this app
> (small images, cached); if it proves janky during verification, switch the
> `Image.Source` bindings to an async loader behavior. Note this in the report.

- [ ] **Step 3: NotConverter**

`BokiIPTV.Avalonia/Converters/NotConverter.cs`:

```csharp
using System.Globalization;
using Avalonia.Data.Converters;
namespace BokiIPTV.Avalonia.Converters;

public sealed class NotConverter : IValueConverter
{
    public static readonly NotConverter Instance = new();
    public object Convert(object? v, Type t, object? p, CultureInfo c) => v is not true;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c) => v is not true;
}
```

- [ ] **Step 4: Build & commit**

Run: `dotnet build BokiIPTV.Avalonia` → `Build succeeded`.

```bash
git add -A && git commit -m "feat(avalonia): AppPaths and converters"
```

---

## Task 4: Avalonia PlayerService + IPlayerService

**Files:**
- Create: `BokiIPTV.Avalonia/Services/IPlayerService.cs`, `BokiIPTV.Avalonia/Services/PlayerService.cs`

**Interfaces:**
- Consumes: `BokiIPTV.Core.Services.PlayerGuard`.
- Produces `IPlayerService` identical in shape to the WPF one EXCEPT `Attach`
  takes the Avalonia VideoView:
  - `void Attach(LibVLCSharp.Avalonia.VideoView view)`
  - `void Play(string url, string? title = null, string? resumeKey = null, long resumeMs = 0)`
  - `string? NowPlayingTitle { get; }`, `string? CurrentKey { get; }`, `bool HasPendingResume { get; }`, `void ApplyResumeIfReady()`
  - `void Stop()`, `void TogglePause()`, `void SetVolume(double v01)`
  - `double Position { get; set; }`, `long TimeMs { get; }`, `long LengthMs { get; }`, `bool IsSeekable { get; }`
  - `void SetBrightness(double v)`, `SetContrast(double v)`, `SetSaturation(double v)`, `SetSpeed(double v)`

- [ ] **Step 1: Port the service**

Copy `BokiIPTV.App/Services/PlayerService.cs` to `BokiIPTV.Avalonia/Services/PlayerService.cs`
verbatim, then change exactly two things:
1. `using LibVLCSharp.WPF;` → `using LibVLCSharp.Avalonia;`
2. nothing else — `MediaPlayer`, `LibVLC`, `Media`, `VideoAdjustOption`, `Core.Initialize()` are all from `LibVLCSharp.Shared` and identical.

`IPlayerService.cs`: copy `BokiIPTV.App/Services/IPlayerService.cs`, change
`using LibVLCSharp.WPF;` → `using LibVLCSharp.Avalonia;`.

- [ ] **Step 2: Build & commit**

Run: `dotnet build BokiIPTV.Avalonia` → `Build succeeded`.

```bash
git add -A && git commit -m "feat(avalonia): VLC player service"
```

---

## Task 5: Port the ViewModels

**Files:**
- Create: `BokiIPTV.Avalonia/ViewModels/{MainViewModel,SectionViewModel,PlayerViewModel,SettingsViewModel,LoginViewModel}.cs`

**Interfaces:**
- Consumes: Core services, `IPlayerService` (Task 4), `ICredentialsProvider` (Task 1).
- Produces: the same VM public surface the WPF app exposes, plus `SettingsViewModel`
  gains live credential apply (Task 8 wires the UI).

- [ ] **Step 1: Copy the five ViewModels from WPF**

Copy each file from `BokiIPTV.App/ViewModels/` to `BokiIPTV.Avalonia/ViewModels/`,
changing the namespace `BokiIPTV.App.ViewModels` → `BokiIPTV.Avalonia.ViewModels`
and `using BokiIPTV.App.Services;` → `using BokiIPTV.Avalonia.Services;`.

- [ ] **Step 2: Fix `PlayerViewModel` timer**

Change `using System.Windows.Threading;` → `using Avalonia.Threading;`.
`DispatcherTimer` API is the same (`Interval`, `Tick`, `Start`). No other change.

- [ ] **Step 3: Fix `LoginViewModel` startup-window handoff**

The WPF `LoginViewModel` uses `App.Services` + `System.Windows.Application.Current.Windows`.
Replace its post-auth navigation with an event the window subscribes to:

```csharp
public event Action? LoginSucceeded;
// ...in LoginAsync, after config.Save(cfg):
LoginSucceeded?.Invoke();
```

Remove the WPF window-closing loop and the `Microsoft.Extensions.DependencyInjection`
window resolution from the VM (the View handles navigation in Task 7).

- [ ] **Step 4: `MainViewModel` credentials via provider**

`MainViewModel` currently builds `XtreamCredentials` from config and passes it to
sections. Change it to take `ICredentialsProvider` and pass `creds.Current` (and
expose `creds` so `SettingsViewModel` can `Update`). The `AddPlaylistAsync`,
download, and section-construction logic is unchanged.

- [ ] **Step 5: `SettingsViewModel` live apply**

Expand `SettingsViewModel` to:

```csharp
using System.Net.Http;
using BokiIPTV.Core.Services;
using BokiIPTV.Core.Xtream;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BokiIPTV.Avalonia.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigService _config;
    private readonly ICredentialsProvider _creds;
    private readonly ICacheService _cache;
    public event Action? CredentialsChanged;   // MainViewModel reloads sections on this

    [ObservableProperty] private string _baseUrl = "";
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string? _status;
    [ObservableProperty] private bool _busy;

    public SettingsViewModel(IConfigService config, ICredentialsProvider creds, ICacheService cache)
    {
        _config = config; _creds = creds; _cache = cache;
        var c = creds.Current;
        BaseUrl = c.BaseUrl; Username = c.Username; Password = c.Password;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        Busy = true; Status = "Checking…";
        try
        {
            var cred = new XtreamCredentials(BaseUrl.Trim(), Username.Trim(), Password);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var info = await new XtreamClient(http, new CredentialsProvider(cred)).AuthenticateAsync(default);
            if (!info.IsActive) { Status = "Login failed: not active / wrong details."; return; }

            var cfg = _config.Load();
            cfg.BaseUrl = cred.BaseUrl; cfg.Username = cred.Username; cfg.Password = cred.Password;
            _config.Save(cfg);
            _creds.Update(cred);
            ClearCache();
            Status = "Saved. Catalogue reloaded.";
            CredentialsChanged?.Invoke();
        }
        catch (Exception ex) { Status = $"Error: {ex.Message}"; }
        finally { Busy = false; }
    }

    [RelayCommand]
    private void RefreshCatalogue() { ClearCache(); Status = "Cache cleared — reopen a section."; CredentialsChanged?.Invoke(); }

    private void ClearCache()
    {
        try { if (System.IO.Directory.Exists(AppPaths.Cache)) System.IO.Directory.Delete(AppPaths.Cache, true); } catch { }
        System.IO.Directory.CreateDirectory(AppPaths.Cache);
    }
}
```

In `MainViewModel`, subscribe to `SettingsViewModel.CredentialsChanged` to rebuild
the Live/Movies/Series sections (clear their `Categories`/`Items` and call
`LoadCategoriesAsync` on the selected one). Expose the `SettingsViewModel` as a
property `Settings` for binding.

- [ ] **Step 6: Build & commit**

Run: `dotnet build BokiIPTV.Avalonia` (will fail until Views exist if VMs reference
view types — they should not; VMs are view-agnostic). If it builds, commit:

```bash
git add -A && git commit -m "feat(avalonia): port view models + live settings apply"
```

---

## Task 6: App bootstrap (Program.cs + App.axaml + DI)

**Files:**
- Modify: `BokiIPTV.Avalonia/Program.cs`, `BokiIPTV.Avalonia/App.axaml`, `BokiIPTV.Avalonia/App.axaml.cs`

**Interfaces:**
- Consumes: all services + VMs.
- Produces: `App.Services` (`IServiceProvider`); shows `LoginWindow` or `MainWindow`.

- [ ] **Step 1: Program.cs**

```csharp
using Avalonia;
namespace BokiIPTV.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
```

- [ ] **Step 2: App.axaml**

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="BokiIPTV.Avalonia.App">
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>
</Application>
```

- [ ] **Step 3: App.axaml.cs — DI host (mirrors WPF App.xaml.cs)**

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BokiIPTV.Avalonia.Services;
using BokiIPTV.Avalonia.ViewModels;
using BokiIPTV.Avalonia.Views;
using BokiIPTV.Core.Services;
using BokiIPTV.Core.Xtream;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BokiIPTV.Avalonia;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private IHost _host = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        _host = Host.CreateDefaultBuilder().ConfigureServices(s =>
        {
            s.AddSingleton<IConfigService>(_ => new ConfigService(AppPaths.Root));
            s.AddSingleton<ICacheService>(_ => new CacheService(AppPaths.Cache));
            s.AddSingleton<IFavoritesService>(_ => new FavoritesService(AppPaths.Root));
            s.AddSingleton<IWatchHistoryService>(_ => new WatchHistoryService(AppPaths.Root));
            s.AddSingleton<IResumeService>(_ => new ResumeService(AppPaths.Root));
            s.AddSingleton<IDownloadService>(_ =>
                new DownloadService(new System.Net.Http.HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan }));
            s.AddSingleton<ICredentialsProvider>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfigService>().Load();
                return new CredentialsProvider(new XtreamCredentials(cfg.BaseUrl, cfg.Username, cfg.Password));
            });
            s.AddHttpClient<IXtreamClient, XtreamClient>();
            s.AddSingleton<IEpgService, EpgService>();
            s.AddSingleton<IPlayerService, PlayerService>();
            s.AddSingleton<MainViewModel>();
            s.AddTransient<LoginViewModel>();
        }).Build();
        Services = _host.Services;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var cfg = Services.GetRequiredService<IConfigService>().Load();
            if (string.IsNullOrWhiteSpace(cfg.Username))
            {
                var login = new LoginWindow { DataContext = Services.GetRequiredService<LoginViewModel>() };
                desktop.MainWindow = login;
            }
            else
            {
                desktop.MainWindow = new MainWindow { DataContext = Services.GetRequiredService<MainViewModel>() };
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build BokiIPTV.Avalonia`
Expected: fails on missing `LoginWindow`/`MainWindow` until Task 7. Proceed.

---

## Task 7: MainWindow + LoginWindow + AddPlaylistWindow (Avalonia XAML)

**Files:**
- Create: `BokiIPTV.Avalonia/Views/MainWindow.axaml(.cs)`, `Views/LoginWindow.axaml(.cs)`, `Views/AddPlaylistWindow.axaml(.cs)`
- Create: `BokiIPTV.Avalonia/Assets/icon.ico` (copy from `BokiIPTV.App/icon.ico`)

**Interfaces:**
- Consumes: `MainViewModel`, `LoginViewModel`, `IPlayerService`.
- Produces: full UI with parity to WPF (`MainWindow.xaml`).

- [ ] **Step 1: Translate MainWindow.axaml from the WPF MainWindow.xaml**

Reproduce `BokiIPTV.App/MainWindow.xaml` in Avalonia, applying the substitution
table at the top of this plan. Key specifics:
- Root `Window` uses `xmlns="https://github.com/avaloniaui"`, `Icon="/Assets/icon.ico"`,
  and a `KeyDown` handler.
- The four-column `Grid` (nav 190 / categories 230 / items `*` / detail 430) is identical.
- Nav/category/items styling: convert WPF `Style`/`Trigger` blocks to Avalonia
  `<Style Selector="...">` with `:pointerover` and `:selected`/`:checked` pseudo-classes.
- Posters: `Image Source="{Binding ..., Converter={x:Static conv:PosterConverter.Instance}}"`.
- Item/episode double-click: WPF `EventSetter MouseDoubleClick` → set
  `DoubleTapped` on the `ListBox` and resolve the item via the handler (Avalonia:
  use `ListBox.DoubleTapped` and `((Control)e.Source).DataContext`).
- VideoView: `<vlc:VideoView Name="Video" />` with
  `xmlns:vlc="clr-namespace:LibVLCSharp.Avalonia;assembly=LibVLCSharp.Avalonia"`.
- The ⚙ adjustments use an Avalonia `Popup`/`Flyout`; the seek/volume `Slider`,
  Play/Pause/Stop, ⛶/🗗 buttons, download progress, and now-playing overlay all
  port directly (use `IsVisible` bindings instead of visibility converters).
- Add a **Settings** entry: a nav button that swaps the detail+player column (or a
  modal `SettingsView`) bound to `MainViewModel.Settings`.

- [ ] **Step 2: MainWindow.axaml.cs (code-behind: fullscreen/PiP/dialogs)**

Port `MainWindow.xaml.cs`. Substitutions:
- `WindowState.FullScreen` (Avalonia has `WindowState.FullScreen`) instead of the
  WPF borderless/maximize trick — simpler and correct cross-platform. Keep the
  panel-collapse for PiP.
- PiP: `Topmost = true`, `WindowState = Normal`, set `Width/Height`, position via
  `Position = new PixelPoint(...)` using `Screens.Primary.WorkingArea`.
- Double-click: `Video` `DoubleTapped` → `ToggleFullscreen`.
- Download/AddPlaylist file pickers: use
  `await GetTopLevel(this)!.StorageProvider.SaveFilePickerAsync(...)` /
  `OpenFilePickerAsync(...)`; convert the returned `IStorageFile` to a path via
  `file.Path.LocalPath`.

- [ ] **Step 3: LoginWindow.axaml(.cs)**

Port `Views/LoginView.xaml`. `PasswordBox` → `TextBox PasswordChar="•"`. On
`LoginViewModel.LoginSucceeded`, open `MainWindow` (resolve `MainViewModel` from
`App.Services`) and close the login window.

- [ ] **Step 4: AddPlaylistWindow.axaml(.cs)**

Port `Views/AddPlaylistWindow.xaml`. The Browse button uses
`StorageProvider.OpenFilePickerAsync` with an `.m3u/.m3u8` file-type filter.

- [ ] **Step 5: Build & run on Windows**

Run: `dotnet build BokiIPTV.Avalonia` → `Build succeeded`.
Run: `dotnet run --project BokiIPTV.Avalonia`
Expected: app launches; if a `config.json` exists it goes straight to the catalogue;
playing a movie shows video; fullscreen, PiP, search, favorites, download, settings work.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(avalonia): main/login/add-playlist windows + settings UI"
```

---

## Task 8: Settings reachable + live reload wired

**Files:**
- Modify: `BokiIPTV.Avalonia/Views/MainWindow.axaml(.cs)`, `BokiIPTV.Avalonia/ViewModels/MainViewModel.cs`

**Interfaces:**
- Consumes: `MainViewModel.Settings` (Task 5), `SettingsViewModel.CredentialsChanged`.

- [ ] **Step 1: Add the Settings entry + view**

In `MainWindow.axaml`, add a **Settings** nav button that shows a `SettingsView`
(fields: Server URL, Username, Password [PasswordChar], Save, Refresh catalogue,
status text) bound to `{Binding Settings}`.

- [ ] **Step 2: Reload sections on change**

In `MainViewModel` constructor, subscribe:

```csharp
Settings.CredentialsChanged += () =>
{
    foreach (var s in Sections.Where(s => s.Kind is SectionKind.Live or SectionKind.Movies or SectionKind.Series))
        s.ResetForReload();           // add a small method that clears Categories/Items
    _ = SelectedSection?.LoadCategoriesAsync();
};
```

Add to `SectionViewModel`:

```csharp
public void ResetForReload() { Categories.Clear(); Items.Clear(); SelectedCategory = null; }
```

- [ ] **Step 3: Build, run, verify**

Run: `dotnet run --project BokiIPTV.Avalonia`
Expected: open Settings, change credentials, Save → re-auth + catalogue reloads
without restart; bad credentials → inline error, nothing changes.

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat(avalonia): in-app settings to update server/user/pass with live reload"
```

---

## Task 9: GitHub Actions — CI

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Write the CI workflow**

```yaml
name: CI
on:
  push: { branches: [ main ] }
  pull_request: { branches: [ main ] }
jobs:
  build-test:
    strategy:
      fail-fast: false
      matrix:
        os: [ windows-latest, macos-latest, ubuntu-latest ]
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - name: Restore
        run: dotnet restore
      - name: Build Core + Avalonia + Tests
        run: dotnet build BokiIPTV.Core BokiIPTV.Avalonia BokiIPTV.Tests -c Release --no-restore
      - name: Test
        run: dotnet test BokiIPTV.Tests -c Release --no-build
```

> The WPF project (`BokiIPTV.App`) is Windows-only; CI builds the specific
> projects rather than the whole solution so macOS/Linux don't try to build WPF.

- [ ] **Step 2: Commit & push, confirm green**

```bash
git add .github/workflows/ci.yml && git commit -m "ci: build + test on Windows/macOS/Linux"
git push
```

Run: `gh run watch` (or check the Actions tab)
Expected: all three OS jobs pass.

---

## Task 10: GitHub Actions — Release packaging

**Files:**
- Create: `.github/workflows/release.yml`

- [ ] **Step 1: Write the release workflow**

```yaml
name: Release
on:
  push: { tags: [ 'v*' ] }
permissions: { contents: write }
jobs:
  publish:
    strategy:
      fail-fast: false
      matrix:
        include:
          - { os: windows-latest, rid: win-x64,  ext: zip }
          - { os: macos-latest,   rid: osx-x64,  ext: zip }
          - { os: macos-latest,   rid: osx-arm64, ext: zip }
          - { os: ubuntu-latest,  rid: linux-x64, ext: tar.gz }
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - name: Publish
        run: dotnet publish BokiIPTV.Avalonia -c Release -r ${{ matrix.rid }} --self-contained true -o out/${{ matrix.rid }}
      - name: Package (zip)
        if: matrix.ext == 'zip'
        run: cd out/${{ matrix.rid }} && 7z a ../../BokiIPTV-${{ matrix.rid }}.zip .
      - name: Package (tar.gz)
        if: matrix.ext == 'tar.gz'
        run: tar -czf BokiIPTV-${{ matrix.rid }}.tar.gz -C out/${{ matrix.rid }} .
      - name: Upload to release
        uses: softprops/action-gh-release@v2
        with: { files: 'BokiIPTV-${{ matrix.rid }}.*' }
```

> `7z` is preinstalled on GitHub windows/macos runners; `tar` on Linux. Linux
> users must have system libVLC installed at runtime — noted in release notes.

- [ ] **Step 2: Commit & push**

```bash
git add .github/workflows/release.yml && git commit -m "ci: cross-platform release packaging on tags"
git push
```

- [ ] **Step 3: Tag a test release**

```bash
git tag v2.0.0-rc1 && git push origin v2.0.0-rc1
```

Run: `gh run watch`
Expected: four packages attached to the `v2.0.0-rc1` release.

---

## Task 11: README + retire WPF (after verification)

**Files:**
- Modify: `README.md`
- Delete: `BokiIPTV.App/` (WPF) and its `dotnet sln remove`
- Modify: `BokiIPTV.sln`

> Only do this task once the Avalonia app is confirmed working on Windows (locally)
> and CI is green on all three OSes. Until then, WPF stays as the fallback.

- [ ] **Step 1: Update README**

Update platform table to ✅ Windows / macOS / Linux. Document the **Linux libVLC
prerequisite** (`sudo apt install vlc` / distro equivalent). Update run/build
instructions to `dotnet run --project BokiIPTV.Avalonia`. Mention the Settings screen.

- [ ] **Step 2: Remove WPF project**

```bash
dotnet sln remove BokiIPTV.App
git rm -r BokiIPTV.App
git commit -m "chore: retire WPF app — Avalonia is now the cross-platform default"
```

- [ ] **Step 3: Final full build + test**

Run: `dotnet build && dotnet test`
Expected: `Build succeeded`; all tests pass.

- [ ] **Step 4: Tag the real release**

```bash
git tag v2.0.0 && git push origin v2.0.0
```

Expected: CI publishes Windows/macOS (x64+arm64)/Linux packages to the v2.0.0 release.

---

## Notes on verification limits

Development happens on Windows: the Avalonia app is verified **functionally on
Windows** here. macOS and Linux are verified by **CI build/test success**; true
runtime confirmation on those OSes requires a real machine or a runner with a
display — call this out in the final report rather than claiming runtime success
on untested platforms.
