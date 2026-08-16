namespace Venguard.Config;

public sealed class VenguardConfig
{
    public bool IsFirstRun { get; set; } = true;

    public bool AutoStart { get; set; }

    public bool LaunchDiscordAfterPatch { get; set; } = true;

    public bool UseOpenAsar { get; set; }
}