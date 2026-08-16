namespace Venguard.Services;

public sealed record DiscordInstallation(
    string DiscordPath,
    string VersionPath,
    string ResourcesPath,
    string AppAsarPath,
    string VencordAsarPath);