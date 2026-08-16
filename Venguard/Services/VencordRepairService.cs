namespace Venguard.Services;

public sealed class VencordRepairService
{
    private readonly DiscordService _discordService;
    private readonly VencordInstallerManager _installerManager;
    private readonly VencordInstallerService _installerService;

    public VencordRepairService(
        DiscordService discordService,
        VencordInstallerManager installerManager,
        VencordInstallerService installerService)
    {
        _discordService = discordService;
        _installerManager = installerManager;
        _installerService = installerService;
    }

    public bool IsDiscordRunning()
    {
        return _discordService.IsDiscordRunning();
    }

    public async Task<VencordRepairResult> RepairAsync(
        CancellationToken cancellationToken = default)
    {
        var installation = _discordService.GetInstallation();

        if (installation is null)
        {
            return new VencordRepairResult(
                false,
                "Discord Stable was not found.",
                string.Empty,
                string.Empty);
        }

        var installerPath = await _installerManager.GetInstallerAsync(
            cancellationToken);

        return await _installerService.RepairAsync(
            installerPath,
            installation.DiscordPath,
            cancellationToken);
    }
}