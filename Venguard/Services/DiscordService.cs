using System.Diagnostics;
using System.IO;
using System.Text;

namespace Venguard.Services;

public sealed class DiscordService
{
    private readonly string _discordPath;

    public DiscordService()
    {
        _discordPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "Discord");
    }

    public DiscordStatus GetStatus()
    {
        var installation = GetInstallation();

        if (installation is null)
        {
            return new DiscordStatus(
                false,
                false,
                false,
                null);
        }

        var isVencordPatched =
            File.Exists(installation.VencordAsarPath);

        var isOpenAsar =
            IsOpenAsar(installation);

        return new DiscordStatus(
            true,
            isVencordPatched,
            isOpenAsar,
            installation.VersionPath);
    }

    public bool IsDiscordRunning()
    {
        return Process.GetProcessesByName(
            "Discord").Length > 0;
    }

    public DiscordInstallation? GetInstallation()
    {
        var versionPath = GetDiscordVersionPath();

        if (versionPath is null)
        {
            return null;
        }

        var resourcesPath = Path.Combine(
            versionPath,
            "resources");

        return new DiscordInstallation(
            _discordPath,
            versionPath,
            resourcesPath,
            Path.Combine(
                resourcesPath,
                "app.asar"),
            Path.Combine(
                resourcesPath,
                "_app.asar"));
    }

    private static bool IsOpenAsar(
        DiscordInstallation installation)
    {
        var candidates = new[]
        {
            installation.AppAsarPath,
            installation.VencordAsarPath
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var bytes = File.ReadAllBytes(path);
                var text = Encoding.UTF8.GetString(bytes);

                if (text.Contains(
                        "OpenAsar",
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }
            catch
            {
                
            }
        }

        return false;
    }

    private string? GetDiscordVersionPath()
    {
        if (!Directory.Exists(_discordPath))
        {
            return null;
        }

        var versions = Directory.GetDirectories(
            _discordPath,
            "app-*");

        return versions
            .OrderByDescending(
                path => path,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(
                HasResourcesDirectory);
    }

    private static bool HasResourcesDirectory(
        string path)
    {
        return Directory.Exists(
            Path.Combine(
                path,
                "resources"));
    }
}