using System.Text.Json;
namespace BokiIPTV.Core.Services;

public sealed class ConfigService : IConfigService
{
    private readonly string _path;
    public ConfigService(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "config.json");
    }

    public AppConfig Load()
        => File.Exists(_path)
            ? JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_path)) ?? new AppConfig()
            : new AppConfig();

    public void Save(AppConfig config)
        => File.WriteAllText(_path, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
}
