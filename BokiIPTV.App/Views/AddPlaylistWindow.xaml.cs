using System.Windows;
using Microsoft.Win32;

namespace BokiIPTV.App;

public partial class AddPlaylistWindow : Window
{
    public string? Source { get; private set; }

    public AddPlaylistWindow() => InitializeComponent();

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "M3U playlists (*.m3u;*.m3u8)|*.m3u;*.m3u8|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) == true) SourceBox.Text = dlg.FileName;
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        var text = SourceBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text)) { SourceBox.Focus(); return; }
        Source = text;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
