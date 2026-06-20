using System.Windows;
using System.Windows.Controls;
using BokiIPTV.App.Services;
using BokiIPTV.App.ViewModels;

namespace BokiIPTV.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

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
}
