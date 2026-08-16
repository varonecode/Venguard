using System.Diagnostics;
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
        var installation = GetInstallation();

        if (installation is null)
        {
            return new DiscordStatus(false, false, null);
        }

        return new DiscordStatus(
            true,
            File.Exists(installation.VencordAsarPath),
            installation.VersionPath);
    }

    public bool IsDiscordRunning()
    {
        return Process.GetProcessesByName("Discord").Length > 0;
    }

    public DiscordInstallation? GetInstallation()
    {
        var versionPath = GetDiscordVersionPath();

        if (versionPath is null)
        {
            return null;
        }

        var resourcesPath = Path.Combine(versionPath, "resources");

        return new DiscordInstallation(
            _discordPath,
            versionPath,
            resourcesPath,
            Path.Combine(resourcesPath, "app.asar"),
            Path.Combine(resourcesPath, "_app.asar"));
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