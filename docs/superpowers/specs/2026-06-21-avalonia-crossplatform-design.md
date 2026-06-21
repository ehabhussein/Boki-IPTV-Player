# BokiIPTV Cross-Platform (Avalonia) — Design Spec

**Date:** 2026-06-21
**Status:** Approved

## 1. Purpose

Make BokiIPTV run on **Windows, macOS, and Linux** by porting the UI from WPF
(Windows-only) to **Avalonia 11** (cross-platform), reusing the existing
`BokiIPTV.Core` library unchanged. Add an in-app **Settings** screen so users can
update the IPTV server URL / username / password without editing JSON. Retire WPF
once the Avalonia build is verified on all three OSes.

## 2. Decisions (from brainstorming)

- **Single Avalonia app** for all three OSes (Option B). WPF stays in the repo,
  frozen as a fallback, until Avalonia is verified — then removed.
- **Feature parity** with the current WPF app (v1.3.0): catalogue (Live/Movies/
  Series), categories, poster-grid, global search, favorites, Recently Watched,
  resume, EPG now/next, M3U import, fullscreen, mini-player/PiP, download, video
  adjustments, now-playing title + hover overlay.
- **Settings screen** ships in the Avalonia app only.
- **GitHub Actions CI** builds/tests on Windows+macOS+Linux and publishes
  per-RID packages on tags.
- ViewModels are **ported (copied + adjusted)** into the Avalonia app, not shared
  via a third project. Core remains the single source of business logic.

## 3. Architecture & code reuse

- `BokiIPTV.Core` — unchanged. Pure portable .NET (Xtream client, M3U parser,
  config/cache/favorites/history/resume/download/EPG services, 27 tests).
- New **`BokiIPTV.Avalonia`** (Avalonia 11, `net10.0`) referencing Core.
- `BokiIPTV.App` (WPF) — frozen; remains buildable as a fallback until retirement.
- `BokiIPTV.Tests` — unchanged.

Ported view-layer pieces (WPF → Avalonia equivalents):

| WPF | Avalonia |
|---|---|
| `System.Windows.Threading.DispatcherTimer` | `Avalonia.Threading.DispatcherTimer` |
| `IValueConverter` (`System.Windows.Data`) | `Avalonia.Data.Converters.IValueConverter` |
| `LibVLCSharp.WPF` `VideoView` | `LibVLCSharp.Avalonia` `VideoView` |
| `Microsoft.Win32.SaveFileDialog` / `OpenFileDialog` | Avalonia `IStorageProvider` |
| `Window`, `Popup`, `Grid`, styles | Avalonia XAML equivalents (~90% identical) |
| Generic Host DI bootstrap in `App.xaml.cs` | same `Microsoft.Extensions.*` in Avalonia `App.axaml.cs` |

ViewModels (`MainViewModel`, `SectionViewModel`, `PlayerViewModel`,
`SettingsViewModel`, `LoginViewModel`) are copied into `BokiIPTV.Avalonia/ViewModels`
and adjusted only where they touch the platform timer / player / dialogs.

## 4. Video & native libVLC per OS

- Video surface: `LibVLCSharp.Avalonia` `VideoView`.
- Native libVLC:
  - **Windows:** `VideoLAN.LibVLC.Windows` (bundled NuGet, as today).
  - **macOS:** `VideoLAN.LibVLC.Mac` (bundled NuGet).
  - **Linux:** system libVLC (`libvlc` / `vlc` via the user's package manager).
    No bundled Linux NuGet exists; documented as a runtime prerequisite.
- `LibVLCSharp.Shared.Core.Initialize()` continues to locate the native libs.

## 5. Settings screen

- A **Settings** entry in the left nav (alongside Live/Movies/Series/Favorites/
  Recently Watched). It is a distinct view (not a catalogue section).
- Fields: **Server URL**, **Username**, **Password**; buttons **Save** and
  **Refresh catalogue**.
- A mutable **`ICredentialsProvider`** holds the current `XtreamCredentials`.
  `XtreamClient` reads credentials from it per request (instead of a fixed value
  captured at construction).
- **Save** flow: validate by calling `AuthenticateAsync`; on success → persist to
  `config.json`, update the credentials provider, clear the catalogue cache, and
  reload the Live/Movies/Series sections live (no restart). On auth failure →
  inline error, nothing persisted.
- **Refresh catalogue**: clears cache and reloads the current section (existing
  behavior, now reachable from the UI).

> This requires a small `Core` change: introduce `ICredentialsProvider` and make
> `XtreamClient` consume it. `StreamUrlBuilder` calls read the current credentials.
> This is the one intentional Core change in this project.

## 6. CI & packaging (GitHub Actions)

- `.github/workflows/ci.yml`: matrix on `windows-latest`, `macos-latest`,
  `ubuntu-latest`. Steps: setup .NET 10, `dotnet build`, `dotnet test`
  (Core tests run on every push/PR).
- `.github/workflows/release.yml` (on `v*` tag): `dotnet publish` the Avalonia app
  per RID — `win-x64`, `osx-x64`, `osx-arm64`, `linux-x64` — zip/tar each, and
  upload as GitHub Release assets. Linux notes the libVLC runtime requirement.

## 7. Testing & transition

- Core's 27 xUnit tests run unchanged in CI on all three OSes.
- New Core tests for `ICredentialsProvider` integration with `XtreamClient`.
- Avalonia app verified **functionally on Windows** during development; **macOS +
  Linux** verified via CI build success and manual/runner smoke where possible.
- **Retirement:** once Avalonia is confirmed on all three, a final commit removes
  `BokiIPTV.App` (WPF), updates the README, and makes Avalonia the default app.

## 8. Out of scope

- Mobile (Android/iOS) — Avalonia supports it, but not targeted here.
- Installer/packaging beyond zip/tar (no MSI/dmg/AppImage in v1).
- Bundling libVLC on Linux (system dependency instead).
