using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BokiIPTV.App.Services;
using BokiIPTV.App.ViewModels;
using BokiIPTV.Core.Models;

namespace BokiIPTV.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _fullscreen;
    private GridLength _c0, _c1, _c2, _c3, _detail, _playerRow;
    private WindowStyle _prevStyle;
    private WindowState _prevState;
    private ResizeMode _prevResize;

    public MainWindow(MainViewModel vm, IPlayerService player)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += async (_, _) =>
        {
            player.Attach(Video);
            await vm.Sections[0].LoadCategoriesAsync();
        };
    }

    private void ItemsList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Only act when a real item was double-clicked (not empty space / scrollbar).
        if (e.OriginalSource is DependencyObject d && ItemFromVisual(d) is { } item)
            _vm.SelectedSection?.PlayCommand.Execute(item);
    }

    private static object? ItemFromVisual(DependencyObject d)
    {
        while (d is not null and not ListBoxItem) d = System.Windows.Media.VisualTreeHelper.GetParent(d);
        return (d as ListBoxItem)?.DataContext;
    }

    private void Episode_DoubleClick(object sender, RoutedEventArgs e)
    {
        if (sender is ListBoxItem { DataContext: Episode ep })
            _vm.SelectedSection?.PlayEpisodeCommand.Execute(ep);
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void Video_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) ToggleFullscreen();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11) ToggleFullscreen();
        else if (e.Key == Key.Escape && _fullscreen) ToggleFullscreen();
    }

    private void ToggleFullscreen()
    {
        if (!_fullscreen)
        {
            _c0 = Col0.Width; _c1 = Col1.Width; _c2 = Col2.Width; _c3 = Col3.Width;
            _detail = DetailRow.Height; _playerRow = PlayerRow.Height;
            _prevStyle = WindowStyle; _prevState = WindowState; _prevResize = ResizeMode;

            Col0.Width = new GridLength(0);
            Col1.Width = new GridLength(0);
            Col2.Width = new GridLength(0);
            Col3.Width = new GridLength(1, GridUnitType.Star);        // player column fills full width
            DetailRow.Height = new GridLength(0);
            PlayerRow.Height = new GridLength(1, GridUnitType.Star);   // video fills full height
            DetailPanel.Visibility = Visibility.Collapsed;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _fullscreen = true;
        }
        else
        {
            Col0.Width = _c0; Col1.Width = _c1; Col2.Width = _c2; Col3.Width = _c3;
            DetailRow.Height = _detail; PlayerRow.Height = _playerRow;
            DetailPanel.Visibility = Visibility.Visible;

            WindowStyle = _prevStyle;
            ResizeMode = _prevResize;
            WindowState = _prevState;
            _fullscreen = false;
        }
    }
}
