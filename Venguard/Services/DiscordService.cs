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

    public bool IsDiscordInstalled()
    {
        return GetDiscordVersionPath() is not null;
    }

    public bool IsVencordPatched()
    {
        var versionPath = GetDiscordVersionPath();

        if (versionPath is null)
        {
            return false;
        }

        var appAsarPath = Path.Combine(
            versionPath,
            "resources",
            "_app.asar");

        return File.Exists(appAsarPath);
    }

    public string? GetInstallationPath()
    {
        return GetDiscordVersionPath();
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