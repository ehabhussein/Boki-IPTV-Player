using System.Collections.ObjectModel;
using System.Net.Http;
using BokiIPTV.App.Services;
using BokiIPTV.Core.Playlist;
using BokiIPTV.Core.Services;
using BokiIPTV.Core.Xtream;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BokiIPTV.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IXtreamClient _client;
    private readonly ICacheService _cache;
    private readonly IFavoritesService _favs;
    private readonly IWatchHistoryService _history;
    private readonly IResumeService _resume;
    private readonly IEpgService _epg;
    private readonly IPlayerService _player;
    private readonly IConfigService _config;
    private readonly XtreamCredentials _cred;

    public ObservableCollection<SectionViewModel> Sections { get; } = [];
    [ObservableProperty] private SectionViewModel? _selectedSection;
    [ObservableProperty] private string? _playlistStatus;
    public PlayerViewModel Player { get; }

    public MainViewModel(IXtreamClient client, ICacheService cache, IFavoritesService favs,
        IWatchHistoryService history, IResumeService resume, IEpgService epg, IPlayerService player,
        IConfigService config)
    {
        _client = client; _cache = cache; _favs = favs; _history = history; _resume = resume;
        _epg = epg; _player = player; _config = config;
        var cfg = config.Load();
        _cred = new XtreamCredentials(cfg.BaseUrl, cfg.Username, cfg.Password);
        Player = new PlayerViewModel(player, resume) { Volume = cfg.Volume };

        Sections.Add(NewSection(SectionKind.Live, "Live TV"));
        Sections.Add(NewSection(SectionKind.Movies, "Movies"));
        Sections.Add(NewSection(SectionKind.Series, "Series"));
        Sections.Add(NewSection(SectionKind.Favorites, "Favorites"));
        Sections.Add(NewSection(SectionKind.History, "Recently Watched"));
        SelectedSection = Sections[0];

        if (!string.IsNullOrWhiteSpace(cfg.M3uSource))
            _ = AddPlaylistAsync(cfg.M3uSource!, select: false);
    }

    private SectionViewModel NewSection(SectionKind kind, string title, IReadOnlyList<BokiIPTV.Core.Models.M3uEntry>? pl = null)
        => new(kind, title, _client, _cache, _favs, _history, _resume, _epg, _player, _cred, pl);

    partial void OnSelectedSectionChanged(SectionViewModel? value) => _ = value?.LoadCategoriesAsync();

    /// Loads an M3U playlist (URL or file path), adds/replaces the Playlist section, persists the source.
    public async Task AddPlaylistAsync(string source, bool select = true)
    {
        PlaylistStatus = "Loading playlist…";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var entries = await M3uLoader.LoadAsync(source, http);
            if (entries.Count == 0) { PlaylistStatus = "Playlist was empty or unreadable."; return; }

            // Remove any existing playlist section first.
            for (int i = Sections.Count - 1; i >= 0; i--)
                if (Sections[i].Kind == SectionKind.Playlist) Sections.RemoveAt(i);

            var section = NewSection(SectionKind.Playlist, "Playlist", entries);
            Sections.Add(section);

            var cfg = _config.Load();
            cfg.M3uSource = source;
            _config.Save(cfg);

            PlaylistStatus = $"Loaded {entries.Count} streams.";
            if (select) SelectedSection = section;
        }
        catch (Exception ex) { PlaylistStatus = $"Playlist failed: {ex.Message}"; }
    }
}
