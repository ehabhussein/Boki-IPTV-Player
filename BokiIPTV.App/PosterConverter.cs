using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace BokiIPTV.App;

/// Turns a (possibly null/invalid/unreachable) image URL string into a BitmapImage,
/// returning null on any failure so a broken poster never throws or blocks the UI.
public sealed class PosterConverter : IValueConverter
{
    public static readonly PosterConverter Instance = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url)) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.UriSource = uri;
            bmp.EndInit();
            return bmp;
        }
        catch { return null; }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
