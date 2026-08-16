using System.Diagnostics;
using System.IO;

namespace Venguard.Services;

public sealed class VencordInstallerService
{
    public async Task<VencordRepairResult> RepairAsync(
        string installerPath,
        string discordPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(installerPath))
        {
            return new VencordRepairResult(
                false,
                "Vencord installer was not found.",
                string.Empty,
                string.Empty);
        }

        if (!Directory.Exists(discordPath))
        {
            return new VencordRepairResult(
                false,
                "Discord installation was not found.",
                string.Empty,
                string.Empty);
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(installerPath) ?? string.Empty
            };

            startInfo.ArgumentList.Add("--repair");
            startInfo.ArgumentList.Add("--location");
            startInfo.ArgumentList.Add(discordPath);

            var process = new Process
            {
                StartInfo = startInfo
            };

            if (!process.Start())
            {
                return new VencordRepairResult(
                    false,
                    "Failed to start VencordInstallerCli.exe.",
                    string.Empty,
                    string.Empty);
            }

            using (process)
            {
                var standardOutputTask = process.StandardOutput.ReadToEndAsync(
                    cancellationToken);

                var standardErrorTask = process.StandardError.ReadToEndAsync(
                    cancellationToken);

                await process.WaitForExitAsync(cancellationToken);

                var standardOutput = await standardOutputTask;
                var standardError = await standardErrorTask;

                if (process.ExitCode == 0)
                {
                    return new VencordRepairResult(
                        true,
                        "Vencord repair completed successfully.",
                        standardOutput,
                        standardError);
                }

                return new VencordRepairResult(
                    false,
                    $"Vencord installer exited with code {process.ExitCode}.",
                    standardOutput,
                    standardError);
            }
        }
        catch (Exception ex)
        {
            return new VencordRepairResult(
                false,
                ex.ToString(),
                string.Empty,
                string.Empty);
        }
    }
}

public sealed record VencordRepairResult(
    bool Success,
    string Message,
    string Output,
    string Error);