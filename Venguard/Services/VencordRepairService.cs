namespace Venguard.Services;

public sealed class VencordRepairService
{
    private readonly DiscordService _discordService;
    private readonly VencordInstallerManager _installerManager;
    private readonly VencordInstallerService _installerService;
    private readonly OpenAsarService _openAsarService;

    public VencordRepairService(
        DiscordService discordService,
        VencordInstallerManager installerManager,
        VencordInstallerService installerService,
        OpenAsarService openAsarService)
    {
        _discordService = discordService;
        _installerManager = installerManager;
        _installerService = installerService;
        _openAsarService = openAsarService;
    }

    public bool IsDiscordRunning()
    {
        return _discordService.IsDiscordRunning();
    }

    public async Task<VencordRepairResult> RepairAsync(
        bool useOpenAsar,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report("Checking the Discord installation...");

        var installation = _discordService.GetInstallation();

        if (installation is null)
        {
            return new VencordRepairResult(
                false,
                "Discord Stable was not found.",
                string.Empty,
                string.Empty);
        }

        var installerPath =
            await _installerManager.GetInstallerAsync(
                progress,
                cancellationToken);

        progress?.Report("Repairing Vencord...");

        var repairResult =
            await _installerService.RepairAsync(
                installerPath,
                installation.DiscordPath,
                progress,
                cancellationToken);

        if (!repairResult.Success)
        {
            return repairResult;
        }

        var currentStatus = _discordService.GetStatus();

        if (currentStatus.IsOpenAsar == useOpenAsar)
        {
            progress?.Report(
                useOpenAsar
                    ? "OpenAsar is already enabled."
                    : "OpenAsar is already disabled.");

            progress?.Report(
                "Repair completed successfully.");

            return repairResult;
        }

        progress?.Report(
            useOpenAsar
                ? "Installing OpenAsar..."
                : "Removing OpenAsar...");

        var openAsarResult =
            await _openAsarService.SetEnabledAsync(
                installerPath,
                installation.DiscordPath,
                useOpenAsar,
                progress,
                cancellationToken);

        if (!openAsarResult.Success)
        {
            return new VencordRepairResult(
                false,
                openAsarResult.Message,
                repairResult.Output,
                openAsarResult.Error);
        }

        progress?.Report(
            "Repair completed successfully.");

        return new VencordRepairResult(
            true,
            "Vencord repair completed successfully.",
            $"{repairResult.Output}{Environment.NewLine}{openAsarResult.Output}",
            $"{repairResult.Error}{Environment.NewLine}{openAsarResult.Error}");
    }
}