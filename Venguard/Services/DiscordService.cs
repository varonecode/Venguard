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
        return Directory.Exists(_discordPath);
    }

    public string? GetInstallationPath()
    {
        return IsDiscordInstalled() ? _discordPath : null;
    }
}