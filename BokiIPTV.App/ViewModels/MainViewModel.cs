using System.Collections.ObjectModel;
using BokiIPTV.App.Services;
using BokiIPTV.Core.Services;
using BokiIPTV.Core.Xtream;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BokiIPTV.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<SectionViewModel> Sections { get; } = [];
    [ObservableProperty] private SectionViewModel? _selectedSection;
    public PlayerViewModel Player { get; }

    public MainViewModel(IXtreamClient client, ICacheService cache, IFavoritesService favs,
        IEpgService epg, IPlayerService player, IConfigService config)
    {
        var cfg = config.Load();
        var cred = new XtreamCredentials(cfg.BaseUrl, cfg.Username, cfg.Password);
        Player = new PlayerViewModel(player) { Volume = cfg.Volume };

        Sections.Add(new SectionViewModel(SectionKind.Live, "Live TV", client, cache, favs, epg, player, cred));
        Sections.Add(new SectionViewModel(SectionKind.Movies, "Movies", client, cache, favs, epg, player, cred));
        Sections.Add(new SectionViewModel(SectionKind.Series, "Series", client, cache, favs, epg, player, cred));
        Sections.Add(new SectionViewModel(SectionKind.Favorites, "Favorites", client, cache, favs, epg, player, cred));
        SelectedSection = Sections[0];
    }

    partial void OnSelectedSectionChanged(SectionViewModel? value) => _ = value?.LoadCategoriesAsync();
}
