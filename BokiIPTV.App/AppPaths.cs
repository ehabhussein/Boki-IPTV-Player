using System.IO;

namespace BokiIPTV.App;

public static class AppPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BokiIPTV");
    public static string Cache { get; } = Path.Combine(Root, "cache");
}
