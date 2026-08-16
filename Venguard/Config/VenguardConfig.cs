namespace Venguard.Config;

public sealed class VenguardConfig
{
    public bool IsFirstRun { get; set; } = true;

    public bool AutoStart { get; set; } = true;

    public bool StartMinimized { get; set; } = false;

    public bool MinimizeToTray { get; set; } = true;

    public bool CloseToTray { get; set; } = true;

    public bool LaunchDiscordAfterPatch { get; set; } = true;

    public bool UseOpenAsar { get; set; } = true;

    public bool ConfirmBeforeRepair { get; set; } = true;

    public bool EnableNotifications { get; set; } = true;

    public bool NotifyOnRepairSuccess { get; set; } = true;

    public bool NotifyOnRepairFailure { get; set; } = true;

    public int MonitorIntervalSeconds { get; set; } = 10;
}