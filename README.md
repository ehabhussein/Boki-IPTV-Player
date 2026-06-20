# BokiIPTV Player

A clean, modern Windows desktop IPTV player built with **.NET 10** and **WPF**, powered by **libVLC**. Connects to any **Xtream Codes** account (the same backend IPTV Smarters uses) or any **M3U / M3U8** playlist.

> Bring your own IPTV subscription. This app ships **no** channels, streams, or credentials — it's just a player.

## Features

- **Live TV, Movies, and Series** — browses your provider's own categories ("playlists")
- **Poster-wall UI** — cover art for movies/series, channel logos, dark theme
- **Series support** — full season/episode listing, double-click an episode to play
- **Movie detail** — poster, plot, genre, director, rating, duration
- **EPG** — now/next for live channels (degrades gracefully when a channel has no guide data)
- **Global search** — searches the entire catalogue, not just the open category
- **Favorites** — star anything; a Favorites tab that persists across restarts
- **M3U / M3U8 playlists** — add a playlist by URL or local file
- **Player** — seek bar + time for VOD, volume, play/pause/stop
- **True fullscreen** (double-click / F11) and a **mini-player / Picture-in-Picture** mode (always-on-top, fills the window) so you can watch while you work
- **Now-playing title** in the window title bar and as a hover overlay on the video
- **Local caching** of catalogues (refreshes on startup and every 3 hours), single-connection-safe playback

## Requirements

- **Windows 10/11** (WPF — see [Platform support](#platform-support))
- **[.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)** (for the framework-dependent build)

## Run

Download a release, unzip, and run `BokiIPTV.App.exe`.

Or build from source:

```bash
dotnet build
dotnet run --project BokiIPTV.App
```

On first launch, enter your **server URL**, **username**, and **password** (Xtream Codes), or click **＋ Add M3U Playlist** to load an M3U URL/file.

## Configuration

Settings live at `%AppData%\BokiIPTV\config.json`. See [`config.sample.json`](config.sample.json) for the shape:

```json
{
  "BaseUrl": "http://your-server.example:8080",
  "Username": "your_username",
  "Password": "your_password",
  "Volume": 1.0,
  "M3uSource": null
}
```

> The password is stored in **plain text** by design (so it can be hand-edited). Keep this file private.

## Platform support

| Platform | Status |
|---|---|
| Windows x64 / ARM64 | ✅ Supported |
| macOS / Linux | ❌ Not supported — the UI is **WPF**, which is Windows-only |

The UI layer (`BokiIPTV.App`) is WPF and runs **only on Windows**. The `BokiIPTV.Core` library (Xtream API client, M3U parser, caching, favorites) is plain cross-platform .NET — a future **Avalonia** front-end could reuse it to target macOS and Linux.

## Project layout

```
BokiIPTV.Core    Models, Xtream Codes API client, M3U parser, services (UI-free, tested)
BokiIPTV.App     WPF desktop app (MVVM) + libVLC playback
BokiIPTV.Tests   xUnit tests for Core
```

## License

MIT — see [LICENSE](LICENSE).
