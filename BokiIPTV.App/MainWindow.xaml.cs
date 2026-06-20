using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BokiIPTV.App.Services;
using BokiIPTV.App.ViewModels;
using BokiIPTV.Core.Models;

namespace BokiIPTV.App;

public partial class MainWindow : Window
{
    private enum ViewMode { Normal, Fullscreen, Pip }

    private readonly MainViewModel _vm;
    private ViewMode _mode = ViewMode.Normal;

    // Saved layout/window state to restore from Fullscreen or PiP.
    private GridLength _c0, _c1, _c2, _c3, _detail, _playerRow;
    private WindowStyle _prevStyle;
    private WindowState _prevState;
    private ResizeMode _prevResize;
    private bool _prevTopmost;
    private double _prevWidth, _prevHeight, _prevLeft, _prevTop;

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
    private void Pip_Click(object sender, RoutedEventArgs e) => TogglePip();

    private async void AddPlaylist_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AddPlaylistWindow { Owner = this };
        if (dlg.ShowDialog() == true && dlg.Source is { } src)
            await _vm.AddPlaylistAsync(src);
    }

    private void Video_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) ToggleFullscreen();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11) ToggleFullscreen();
        else if (e.Key == Key.Escape && _mode != ViewMode.Normal) RestoreNormal();
    }

    private void ToggleFullscreen()
    {
        if (_mode == ViewMode.Fullscreen) { RestoreNormal(); return; }
        if (_mode == ViewMode.Pip) RestoreNormal();

        SaveLayout();
        CollapseToPlayer();
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowState = WindowState.Maximized;
        Topmost = false;
        _mode = ViewMode.Fullscreen;
    }

    private void TogglePip()
    {
        if (_mode == ViewMode.Pip) { RestoreNormal(); return; }
        if (_mode == ViewMode.Fullscreen) RestoreNormal();

        SaveLayout();
        CollapseToPlayer();
        // Compact floating, always-on-top mini player parked bottom-right.
        WindowState = WindowState.Normal;
        WindowStyle = WindowStyle.SingleBorderWindow;
        ResizeMode = ResizeMode.CanResize;
        Topmost = true;
        Width = 500;
        Height = 300;
        Left = SystemParameters.WorkArea.Right - Width - 24;
        Top = SystemParameters.WorkArea.Bottom - Height - 24;
        _mode = ViewMode.Pip;
    }

    private void SaveLayout()
    {
        _c0 = Col0.Width; _c1 = Col1.Width; _c2 = Col2.Width; _c3 = Col3.Width;
        _detail = DetailRow.Height; _playerRow = PlayerRow.Height;
        _prevStyle = WindowStyle; _prevState = WindowState; _prevResize = ResizeMode; _prevTopmost = Topmost;
        _prevWidth = Width; _prevHeight = Height; _prevLeft = Left; _prevTop = Top;
    }

    private void CollapseToPlayer()
    {
        Col0.Width = new GridLength(0);
        Col1.Width = new GridLength(0);
        Col2.Width = new GridLength(0);
        Col3.Width = new GridLength(1, GridUnitType.Star);
        DetailRow.Height = new GridLength(0);
        PlayerRow.Height = new GridLength(1, GridUnitType.Star);
        DetailPanel.Visibility = Visibility.Collapsed;
    }

    private void RestoreNormal()
    {
        Col0.Width = _c0; Col1.Width = _c1; Col2.Width = _c2; Col3.Width = _c3;
        DetailRow.Height = _detail; PlayerRow.Height = _playerRow;
        DetailPanel.Visibility = Visibility.Visible;

        WindowStyle = _prevStyle;
        ResizeMode = _prevResize;
        Topmost = _prevTopmost;
        WindowState = _prevState;
        Width = _prevWidth;
        Height = _prevHeight;
        Left = _prevLeft;
        Top = _prevTop;
        _mode = ViewMode.Normal;
    }
}
