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
    private GridLength _c0, _c1, _c2, _detail;
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

    private void Item_DoubleClick(object sender, RoutedEventArgs e)
    {
        if (sender is ListBoxItem { DataContext: { } item })
            _vm.SelectedSection?.PlayCommand.Execute(item);
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
            _c0 = Col0.Width; _c1 = Col1.Width; _c2 = Col2.Width; _detail = DetailRow.Height;
            _prevStyle = WindowStyle; _prevState = WindowState; _prevResize = ResizeMode;

            Col0.Width = new GridLength(0);
            Col1.Width = new GridLength(0);
            Col2.Width = new GridLength(0);
            DetailRow.Height = new GridLength(0);
            DetailPanel.Visibility = Visibility.Collapsed;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _fullscreen = true;
        }
        else
        {
            Col0.Width = _c0; Col1.Width = _c1; Col2.Width = _c2; DetailRow.Height = _detail;
            DetailPanel.Visibility = Visibility.Visible;

            WindowStyle = _prevStyle;
            ResizeMode = _prevResize;
            WindowState = _prevState;
            _fullscreen = false;
        }
    }
}
