# BokiIPTV — Design Spec

**Date:** 2026-06-20
**Status:** Approved (design phase)

## 1. Purpose

A Windows desktop app (WPF, .NET 10) to watch an IPTV subscription served via the
**Xtream Codes API** — the same backend IPTV Smarters uses. Replaces the need for
IPTV Smarters by connecting directly to the provider with URL + username + password.

### Verified provider facts (live, 2026-06-20)

Server `http://your-server.example:8080`, user `demo_user`:

- Auth endpoint returns `auth:1`, status `Active`, **`max_connections: 1`**,
  `allowed_output_formats: ["m3u8","ts"]`.
- `server_info` reports streaming port **2095** (differs from login port 8080).
- Catalog scale: **200 live categories, 126 VOD categories, 77 series categories**
  (thousands of items total) → caching + UI virtualization are mandatory.
- Many live channels have `epg_channel_id: null` and empty `get_short_epg` →
  EPG must degrade gracefully to "No guide data".

## 2. Tech stack

- **.NET 10**, **WPF**, **MVVM** via `CommunityToolkit.Mvvm`
  (`[ObservableProperty]`, `[RelayCommand]`).
- **LibVLCSharp** + `LibVLCSharp.WPF` + `VideoLAN.LibVLC.Windows` — video engine
  (handles HLS / MPEG-TS / MP4).
- **Microsoft.Extensions.Hosting / DependencyInjection / Http** — DI, typed
  `HttpClient`, configuration.
- **System.Text.Json** — API + cache serialization.
- **xUnit** — tests for Core (API parsing, URL building, cache logic).

## 3. Solution layout

```
BokiIPTV.sln
├── BokiIPTV.Core        (class library, no UI)
│   ├── Models           Channel, Movie, Series, Episode, Category, EpgEntry, UserInfo
│   ├── Xtream           XtreamClient, StreamUrlBuilder
│   ├── Services         ConfigService, FavoritesService, EpgService, CacheService
│   └── Abstractions     interfaces (IXtreamClient, ICacheService, ...)
├── BokiIPTV.App         (WPF desktop app)
│   ├── Views            LoginView, MainWindow, PlayerView, SettingsView
│   ├── ViewModels       one per view
│   ├── Controls         VlcPlayerControl, EpgStrip, PosterCard
│   └── App.xaml(.cs)    Generic Host + DI bootstrap
└── BokiIPTV.Tests       (xUnit)
```

## 4. Xtream API integration

Typed `HttpClient` wrapper `XtreamClient` (interface `IXtreamClient`). All calls hit
`player_api.php?username=&password=&action=…`.

| Method | action | Returns |
|---|---|---|
| `AuthenticateAsync` | *(none)* | UserInfo (status, expiry, max_connections) |
| `GetLiveCategoriesAsync` | `get_live_categories` | categories |
| `GetLiveStreamsAsync(catId)` | `get_live_streams` | channels |
| `GetVodCategoriesAsync` | `get_vod_categories` | categories |
| `GetVodStreamsAsync(catId)` | `get_vod_streams` | movies |
| `GetSeriesCategoriesAsync` | `get_series_categories` | categories |
| `GetSeriesAsync(catId)` | `get_series` | series |
| `GetSeriesInfoAsync(id)` | `get_series_info` | seasons + episodes |
| `GetShortEpgAsync(streamId)` | `get_short_epg` | now/next |
| `GetXmltvAsync` | `xmltv.php` | full EPG (XML) |

**Stream URLs** are constructed by `StreamUrlBuilder`, never fetched:

- Live:    `{server}/live/{user}/{pass}/{stream_id}.ts`
- Movie:   `{server}/movie/{user}/{pass}/{stream_id}.{container_extension}`
- Episode: `{server}/series/{user}/{pass}/{episode_id}.{container_extension}`

## 5. UI & EPG

- `LoginView` → `MainWindow` with left nav: **Live TV / Movies / Series / Favorites /
  Settings**.
- Each section: category list (left) + **virtualized** item grid/list (right).
- `PlayerView`: LibVLCSharp surface + controls (play/pause, volume, fullscreen,
  channel up/down, stop). Search box per section.
- EPG: now/next strip on live channels + guide panel. `epg_channel_id: null`
  channels show "No guide data" without error.

## 6. Data flow, caching, errors

- **Cache:** catalogs serialized to `%AppData%/BokiIPTV/cache/*.json` with timestamp.
  Refresh **on startup and every 3 hours**; manual "refresh" button forces it.
- **Single-connection guard:** account is `max_connections: 1`. Player **must stop
  the current stream before opening a new one**, or the server rejects the new
  connection. Centralized in the player service.
- **Errors:** auth fail → friendly message; timeout → retry with backoff; dead
  stream → "channel unavailable" overlay (no freeze).
- **Config:** `%AppData%/BokiIPTV/config.json`. Password stored **plain text**
  (user choice — wants to hand-edit). Holds server URL, user, pass, last section,
  volume, favorites reference.

## 7. Testing

- xUnit over `BokiIPTV.Core`: JSON deserialization of each API shape (recorded
  sample payloads), `StreamUrlBuilder` output, cache freshness logic, favorites
  add/remove. UI is thin (MVVM) so logic lives in testable Core.

## 8. Out of scope (YAGNI for v1)

- Catch-up / archive playback (`tv_archive`) — note it exists, defer.
- Multi-profile / multiple providers.
- Recording / DVR.
