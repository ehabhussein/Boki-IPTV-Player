using System.Collections.ObjectModel;
using BokiIPTV.App.Services;
using BokiIPTV.Core.Models;
using BokiIPTV.Core.Services;
using BokiIPTV.Core.Xtream;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BokiIPTV.App.ViewModels;

public enum SectionKind { Live, Movies, Series, Favorites }

public partial class SectionViewModel : ObservableObject
{
    private readonly IXtreamClient _client;
    private readonly ICacheService _cache;
    private readonly IFavoritesService _favs;
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

    public SectionViewModel(SectionKind kind, string title, IXtreamClient client, ICacheService cache,
        IFavoritesService favs, IEpgService epg, IPlayerService player, XtreamCredentials cred)
    {
        Kind = kind; Title = title;
        _client = client; _cache = cache; _favs = favs; _epg = epg; _player = player; _cred = cred;
    }

    public async Task LoadCategoriesAsync()
    {
        if (Kind == SectionKind.Favorites) { LoadFavorites(); return; }
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

    public async Task LoadItemsAsync()
    {
        if (Kind == SectionKind.Favorites) { LoadFavorites(); return; }
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
                _ => []
            };
            if (!string.IsNullOrWhiteSpace(SearchText))
                items = items.Where(i => Name(i).Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            foreach (var i in items) Items.Add(i);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"LoadItems failed: {ex.Message}"); }
        finally { Loading = false; }
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
        CanPlaySelected = item is Channel or Movie || item is FavoriteEntry { Kind: "live" or "vod" };

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

                case FavoriteEntry fe:
                    DetailPoster = fe.Icon;
                    break;
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"LoadDetail failed: {ex.Message}"); }
    }

    private static string Name(object o) => o switch
    {
        Channel c => c.Name, Movie m => m.Name, Series s => s.Name, FavoriteEntry f => f.Title, _ => ""
    };

    private static string? KeyOf(object o) => o switch
    {
        Channel c => $"live:{c.StreamId}",
        Movie m => $"vod:{m.StreamId}",
        Series s => $"series:{s.SeriesId}",
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
        var url = item switch
        {
            Channel c => StreamUrlBuilder.Live(_cred, c.StreamId),
            Movie m => StreamUrlBuilder.Movie(_cred, m.StreamId, m.ContainerExtension),
            FavoriteEntry { Kind: "live" } f => StreamUrlBuilder.Live(_cred, f.StreamId),
            FavoriteEntry { Kind: "vod" } f => StreamUrlBuilder.Movie(_cred, f.StreamId, f.Ext),
            _ => null
        };
        if (url is not null) _player.Play(url);
    }

    [RelayCommand]
    private void PlayEpisode(Episode? ep)
    {
        if (ep is null) return;
        _player.Play(StreamUrlBuilder.Episode(_cred, ep.Id, ep.ContainerExtension));
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
            FavoriteEntry f => f,
            _ => null
        };
        if (entry is null) return;
        IsFavorited = _favs.Toggle(entry);
        if (Kind == SectionKind.Favorites) LoadFavorites();
    }
}
