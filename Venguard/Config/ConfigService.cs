using System.IO;
using System.Text.Json;

namespace Venguard.Config;

public sealed class ConfigService
{
    private readonly string _configDirectory;
    private readonly string _configPath;

    public ConfigService()
    {
        _configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Venguard");

        _configPath = Path.Combine(_configDirectory, "config.json");
    }

    public VenguardConfig Load()
    {
        if (!File.Exists(_configPath))
        {
            return new VenguardConfig();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<VenguardConfig>(json)
                ?? new VenguardConfig();
        }
        catch (JsonException)
        {
            return new VenguardConfig();
        }
    }

    public void Save(VenguardConfig config)
    {
        Directory.CreateDirectory(_configDirectory);

        var json = JsonSerializer.Serialize(
            config,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(_configPath, json);
    }
}