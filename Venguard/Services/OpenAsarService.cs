using System.Diagnostics;
using System.IO;

namespace Venguard.Services;

public sealed class OpenAsarService
{
    public async Task<bool> SetEnabledAsync(
        string installerPath,
        string discordPath,
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(installerPath))
        {
            return false;
        }

        if (!Directory.Exists(discordPath))
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory =
                Path.GetDirectoryName(installerPath) ?? string.Empty
        };

        startInfo.ArgumentList.Add(
            enabled
                ? "--install-openasar"
                : "--uninstall-openasar");

        startInfo.ArgumentList.Add("--location");
        startInfo.ArgumentList.Add(discordPath);

        using var process = new Process
        {
            StartInfo = startInfo
        };

        try
        {
            if (!process.Start())
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}