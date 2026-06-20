using System.Collections.ObjectModel;
using BokiIPTV.App.Services;
using BokiIPTV.Core.Models;
using BokiIPTV.Core.Services;
using BokiIPTV.Core.Xtream;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BokiIPTV.App.ViewModels;

public enum SectionKind { Live, Movies, Series, Favorites, Playlist, History }

public partial class SectionViewModel : ObservableObject
{
    private readonly IXtreamClient _client;
    private readonly ICacheService _cache;
    private readonly IFavoritesService _favs;
    private readonly IWatchHistoryService _history;
    private readonly IResumeService _resume;
    private readonly IEpgService _epg;
    private readonly IPlayerService _player;
    private readonly XtreamCredentials _cred;

    public SectionKind Kind { get; }
    public string Title { get; }
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<object> Items { get; } = [];
    public ObservableCollection<Episode> Episodes { get; } = [];

    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private object? _selectedItem;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _loading;

    // Detail panel state
    [ObservableProperty] private string? _detailTitle;
    [ObservableProperty] private string? _detailPlot;
    [ObservableProperty] private string? _detailPoster;
    [ObservableProperty] private string? _detailMeta;
    [ObservableProperty] private string? _nowNext;
    [ObservableProperty] private bool _hasEpisodes;
    [ObservableProperty] private bool _canPlaySelected;
    [ObservableProperty] private bool _isFavorited;

    private readonly List<M3uEntry> _playlist;

    public SectionViewModel(SectionKind kind, string title, IXtreamClient client, ICacheService cache,
        IFavoritesService favs, IWatchHistoryService history, IResumeService resume, IEpgService epg,
        IPlayerService player, XtreamCredentials cred, IReadOnlyList<M3uEntry>? playlist = null)
    {
        Kind = kind; Title = title;
        _client = client; _cache = cache; _favs = favs; _history = history; _resume = resume;
        _epg = epg; _player = player; _cred = cred;
        _playlist = playlist?.ToList() ?? [];
    }

    public async Task LoadCategoriesAsync()
    {
        if (Kind == SectionKind.Favorites) { LoadFavorites(); return; }
        if (Kind == SectionKind.History) { LoadHistory(); return; }
        if (Kind == SectionKind.Playlist) { LoadPlaylistCategories(); return; }
        if (Categories.Count > 0) return;
        Loading = true;
        try
        {
            var key = $"cat_{Kind}";
            var cats = await _cache.GetAsync<List<Category>>(key) ?? await FetchCategories();
            await _cache.SetAsync(key, cats);
            Categories.Clear();
            foreach (var c in cats) Categories.Add(c);
            SelectedCategory = Categories.FirstOrDefault();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"LoadCategories failed: {ex.Message}"); }
        finally { Loading = false; }
    }

    public void LoadFavorites()
    {
        Items.Clear();
        foreach (var e in _favs.Entries) Items.Add(e);
    }

    public void LoadHistory()
    {
        Items.Clear();
        foreach (var e in _history.Recent) Items.Add(e);
    }

    private void LoadPlaylistCategories()
    {
        if (Categories.Count > 0) return;
        Categories.Clear();
        foreach (var g in _playlist.Select(e => e.Group).Distinct().OrderBy(g => g))
            Categories.Add(new Category { CategoryId = g, CategoryName = g });
        SelectedCategory = Categories.FirstOrDefault();
    }

    private async Task<List<Category>> FetchCategories() => Kind switch
    {
        SectionKind.Live => (await _client.GetLiveCategoriesAsync(default)).ToList(),
        SectionKind.Movies => (await _client.GetVodCategoriesAsync(default)).ToList(),
        SectionKind.Series => (await _client.GetSeriesCategoriesAsync(default)).ToList(),
        _ => []
    };

    partial void OnSelectedCategoryChanged(Category? value) => _ = LoadItemsAsync();
    partial void OnSearchTextChanged(string value) => _ = LoadItemsAsync();
    partial void OnSelectedItemChanged(object? value) => _ = LoadDetailAsync(value);

    private List<object>? _allItems;   // whole-section catalogue, lazily loaded for global search

    public async Task LoadItemsAsync()
    {
        if (Kind == SectionKind.Favorites) { LoadFavorites(); return; }

        // Global search: when the user types, search the ENTIRE section catalogue
        // (every category at once), like IPTV Smarters — not just the open category.
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            await SearchAllAsync(SearchText);
            return;
        }

        Items.Clear();
        if (SelectedCategory is null) return;
        Loading = true;
        try
        {
            IEnumerable<object> items = Kind switch
            {
                SectionKind.Live => await _client.GetLiveStreamsAsync(SelectedCategory.CategoryId, default),
                SectionKind.Movies => await _client.GetVodStreamsAsync(SelectedCategory.CategoryId, default),
                SectionKind.Series => await _client.GetSeriesAsync(SelectedCategory.CategoryId, default),
                SectionKind.Playlist => _playlist.Where(e => e.Group == SelectedCategory.CategoryId),
                _ => []
            };
            // Poster grid isn't virtualized; cap rendered cards. Categories rarely exceed this,
            // and global search reaches anything beyond the cap.
            foreach (var i in items.Take(600)) Items.Add(i);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"LoadItems failed: {ex.Message}"); }
        finally { Loading = false; }
    }

    private async Task SearchAllAsync(string query)
    {
        Loading = true;
        try
        {
            _allItems ??= await LoadAllItemsAsync();
            Items.Clear();
            foreach (var i in _allItems.Where(i => Name(i).Contains(query, StringComparison.OrdinalIgnoreCase)).Take(500))
                Items.Add(i);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Search failed: {ex.Message}"); }
        finally { Loading = false; }
    }

    private async Task<List<object>> LoadAllItemsAsync()
    {
        if (Kind == SectionKind.Playlist) return _playlist.Cast<object>().ToList();
        var cacheKey = $"all_{Kind}";
        var cached = Kind switch
        {
            SectionKind.Live => (await _cache.GetAsync<List<Channel>>(cacheKey))?.Cast<object>().ToList(),
            SectionKind.Movies => (await _cache.GetAsync<List<Movie>>(cacheKey))?.Cast<object>().ToList(),
            SectionKind.Series => (await _cache.GetAsync<List<Series>>(cacheKey))?.Cast<object>().ToList(),
            _ => null
        };
        if (cached is not null) return cached;

        switch (Kind)
        {
            case SectionKind.Live:
                var live = (await _client.GetAllLiveStreamsAsync(default)).ToList();
                await _cache.SetAsync(cacheKey, live);
                return live.Cast<object>().ToList();
            case SectionKind.Movies:
                var vod = (await _client.GetAllVodStreamsAsync(default)).ToList();
                await _cache.SetAsync(cacheKey, vod);
                return vod.Cast<object>().ToList();
            case SectionKind.Series:
                var ser = (await _client.GetAllSeriesAsync(default)).ToList();
                await _cache.SetAsync(cacheKey, ser);
                return ser.Cast<object>().ToList();
            default: return [];
        }
    }

    private async Task LoadDetailAsync(object? item)
    {
        Episodes.Clear();
        HasEpisodes = false;
        NowNext = null;
        DetailPlot = null;
        DetailMeta = null;
        if (item is null) { DetailTitle = null; DetailPoster = null; CanPlaySelected = false; IsFavorited = false; return; }

        DetailTitle = Name(item);
        IsFavorited = _favs.IsFavorite(KeyOf(item) ?? "");
        CanPlaySelected = item is Channel or Movie or M3uEntry
            || (item is FavoriteEntry pf && (pf.Url is not null || pf.Kind is "live" or "vod"));

        try
        {
            switch (item)
            {
                case Channel ch:
                    DetailPoster = ch.StreamIcon;
                    var now = await _epg.GetNowAsync(ch);
                    var up = await _epg.GetUpcomingAsync(ch);
                    NowNext = now is null
                        ? "No guide data for this channel."
                        : $"NOW: {now.Title}" + (up.Count > 0 ? $"\nNEXT: {up[0].Title}" : "");
                    break;

                case Movie m:
                    DetailPoster = m.StreamIcon;
                    var vod = await _client.GetVodInfoAsync(m.StreamId, default);
                    DetailPoster = vod.Info?.MovieImage ?? m.StreamIcon;
                    DetailPlot = vod.Info?.Plot;
                    DetailMeta = Join(vod.Info?.Genre, vod.Info?.Director, vod.Info?.Rating is { Length: > 0 } r ? $"★ {r}" : null,
                        vod.Info?.Duration);
                    break;

                case Series s:
                    DetailPoster = s.Cover;
                    var info = await _client.GetSeriesInfoAsync(s.SeriesId, default);
                    DetailPoster = info.Info?.Cover ?? s.Cover;
                    DetailPlot = info.Info?.Plot ?? s.Plot;
                    DetailMeta = Join(info.Info?.Genre, info.Info?.Director,
                        info.Info?.Rating is { Length: > 0 } sr ? $"★ {sr}" : null);
                    if (info.Episodes is not null)
                        foreach (var season in info.Episodes.OrderBy(kv => int.TryParse(kv.Key, out var n) ? n : 0))
                            foreach (var ep in season.Value)
                                Episodes.Add(ep);
                    HasEpisodes = Episodes.Count > 0;
                    break;

                case M3uEntry me:
                    DetailPoster = me.Logo;
                    DetailMeta = me.Group;
                    break;

                case FavoriteEntry fe:
                    DetailPoster = fe.Icon;
                    break;
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"LoadDetail failed: {ex.Message}"); }
    }

    private static string Name(object o) => o switch
    {
        Channel c => c.Name, Movie m => m.Name, Series s => s.Name,
        M3uEntry e => e.Name, FavoriteEntry f => f.Title, _ => ""
    };

    private static string? KeyOf(object o) => o switch
    {
        Channel c => $"live:{c.StreamId}",
        Movie m => $"vod:{m.StreamId}",
        Series s => $"series:{s.SeriesId}",
        M3uEntry e => $"m3u:{e.Url}",
        FavoriteEntry f => f.Key,
        _ => null
    };

    private static string? Join(params string?[] parts)
    {
        var kept = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        return kept.Length == 0 ? null : string.Join("  •  ", kept);
    }

    [RelayCommand]
    private void Play(object? item)
    {
        item ??= SelectedItem;
        if (item is null) return;
        var url = item switch
        {
            Channel c => StreamUrlBuilder.Live(_cred, c.StreamId),
            Movie m => StreamUrlBuilder.Movie(_cred, m.StreamId, m.ContainerExtension),
            M3uEntry e => e.Url,
            FavoriteEntry f => f.Url                                        // history / m3u favorites carry a URL
                ?? f.Kind switch
                {
                    "live" => StreamUrlBuilder.Live(_cred, f.StreamId),
                    "vod" => StreamUrlBuilder.Movie(_cred, f.StreamId, f.Ext),
                    _ => null
                },
            _ => null
        };
        if (url is null) return;
        var title = Name(item);
        // Live has no meaningful resume; everything else resumes where it left off.
        string? key = item switch
        {
            Channel => null,
            FavoriteEntry { Kind: "live" } => null,
            _ => KeyOf(item)
        };
        var resumeMs = key is not null ? _resume.GetMs(key) : 0;
        _player.Play(url, title, key, resumeMs);
        RecordHistory(item, url, title);
    }

    [RelayCommand]
    private void PlayEpisode(Episode? ep)
    {
        if (ep is null) return;
        var title = DetailTitle is { Length: > 0 } s ? $"{s} — {ep.Display}" : ep.Display;
        var url = StreamUrlBuilder.Episode(_cred, ep.Id, ep.ContainerExtension);
        var key = $"episode:{ep.Id}";
        _player.Play(url, title, key, _resume.GetMs(key));
        _history.Record(new FavoriteEntry
        {
            Key = $"episode:{ep.Id}", Title = title, Kind = "episode",
            Url = url, Icon = DetailPoster, WatchedAt = DateTimeOffset.UtcNow
        });
    }

    // Records movies / episodes / on-demand playlist items into watch history. Skips live TV.
    private void RecordHistory(object item, string url, string title)
    {
        var kind = item switch
        {
            Movie => "vod", M3uEntry => "m3u", FavoriteEntry f => f.Kind, _ => null
        };
        if (kind is null or "live") return;
        var icon = item switch
        {
            Movie m => m.StreamIcon, M3uEntry e => e.Logo, FavoriteEntry f => f.Icon, _ => DetailPoster
        };
        _history.Record(new FavoriteEntry
        {
            Key = KeyOf(item) ?? url, Title = title, Kind = kind,
            Url = url, Icon = icon, WatchedAt = DateTimeOffset.UtcNow
        });
    }

    [RelayCommand]
    private void ToggleFavorite(object? item)
    {
        item ??= SelectedItem;
        var entry = item switch
        {
            Channel c => new FavoriteEntry { Key = $"live:{c.StreamId}", Title = c.Name, Kind = "live", StreamId = c.StreamId, Icon = c.StreamIcon },
            Movie m => new FavoriteEntry { Key = $"vod:{m.StreamId}", Title = m.Name, Kind = "vod", StreamId = m.StreamId, Ext = m.ContainerExtension, Icon = m.StreamIcon },
            Series s => new FavoriteEntry { Key = $"series:{s.SeriesId}", Title = s.Name, Kind = "series", StreamId = s.SeriesId, Icon = s.Cover },
            M3uEntry e => new FavoriteEntry { Key = $"m3u:{e.Url}", Title = e.Name, Kind = "m3u", Url = e.Url, Icon = e.Logo },
            FavoriteEntry f => f,
            _ => null
        };
        if (entry is null) return;
        IsFavorited = _favs.Toggle(entry);
        if (Kind == SectionKind.Favorites) LoadFavorites();
    }
}
