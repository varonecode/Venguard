namespace Venguard.Services;

public sealed record DiscordStatus(
    bool IsInstalled,
    bool IsVencordPatched,
    string? InstallationPath);