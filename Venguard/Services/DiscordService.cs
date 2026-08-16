using System.IO;

namespace Venguard.Services;

public sealed class DiscordService
{
    private readonly string _discordPath;

    public DiscordService()
    {
        _discordPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Discord");
    }

    public DiscordStatus GetStatus()
    {
        var versionPath = GetDiscordVersionPath();

        if (versionPath is null)
        {
            return new DiscordStatus(false, false, null);
        }

        var appAsarPath = Path.Combine(
            versionPath,
            "resources",
            "_app.asar");

        return new DiscordStatus(
            true,
            File.Exists(appAsarPath),
            versionPath);
    }

    private string? GetDiscordVersionPath()
    {
        if (!Directory.Exists(_discordPath))
        {
            return null;
        }

        var versions = Directory.GetDirectories(_discordPath, "app-*");

        return versions
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(HasResourcesDirectory);
    }

    private static bool HasResourcesDirectory(string path)
    {
        return Directory.Exists(Path.Combine(path, "resources"));
    }
}