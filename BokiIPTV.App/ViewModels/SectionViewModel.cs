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
    private readonly IPlayerService _player;
    private readonly XtreamCredentials _cred;

    public SectionKind Kind { get; }
    public string Title { get; }
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<object> Items { get; } = [];

    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private bool _loading;

    public SectionViewModel(SectionKind kind, string title, IXtreamClient client, ICacheService cache,
        IFavoritesService favs, IPlayerService player, XtreamCredentials cred)
    {
        Kind = kind; Title = title;
        _client = client; _cache = cache; _favs = favs; _player = player; _cred = cred;
    }

    public async Task LoadCategoriesAsync()
    {
        if (Kind == SectionKind.Favorites) return;
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

    private async Task<List<Category>> FetchCategories() => Kind switch
    {
        SectionKind.Live => (await _client.GetLiveCategoriesAsync(default)).ToList(),
        SectionKind.Movies => (await _client.GetVodCategoriesAsync(default)).ToList(),
        SectionKind.Series => (await _client.GetSeriesCategoriesAsync(default)).ToList(),
        _ => []
    };

    partial void OnSelectedCategoryChanged(Category? value) => _ = LoadItemsAsync();
    partial void OnSearchTextChanged(string value) => _ = LoadItemsAsync();

    public async Task LoadItemsAsync()
    {
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

    private static string Name(object o) => o switch
    {
        Channel c => c.Name, Movie m => m.Name, Series s => s.Name, _ => ""
    };

    [RelayCommand]
    private void Play(object item)
    {
        var url = item switch
        {
            Channel c => StreamUrlBuilder.Live(_cred, c.StreamId),
            Movie m => StreamUrlBuilder.Movie(_cred, m.StreamId, m.ContainerExtension),
            _ => null
        };
        if (url is not null) _player.Play(url);
    }

    [RelayCommand]
    private void ToggleFavorite(object item)
    {
        var key = item switch
        {
            Channel c => $"live:{c.StreamId}", Movie m => $"vod:{m.StreamId}",
            Series s => $"series:{s.SeriesId}", _ => null
        };
        if (key is not null) _favs.Toggle(key);
    }
}
