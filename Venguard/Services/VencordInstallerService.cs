using System.Diagnostics;
using System.IO;

namespace Venguard.Services;

public sealed class VencordInstallerService
{
    public async Task<bool> RepairAsync(
        string installerPath,
        string discordPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(installerPath) || !Directory.Exists(discordPath))
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("--repair");
        startInfo.ArgumentList.Add("--location");
        startInfo.ArgumentList.Add(discordPath);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode == 0;
    }
}