namespace Venguard.Services;

public sealed record DiscordInstallation(
    string VersionPath,
    string ResourcesPath,
    string AppAsarPath,
    string VencordAsarPath);