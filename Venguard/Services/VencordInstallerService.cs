using System.Diagnostics;
using System.IO;

namespace Venguard.Services;

public sealed class VencordInstallerService
{
    private static readonly TimeSpan RepairTimeout =
        TimeSpan.FromMinutes(2);

    public async Task<VencordRepairResult> RepairAsync(
        string installerPath,
        string discordPath,
        IProgress<string>? progress = null,
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

        var outputLines = new List<string>();
        var errorLines = new List<string>();

        try
        {
            progress?.Report("Starting the official Vencord repair...");

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

            startInfo.ArgumentList.Add("--repair");
            startInfo.ArgumentList.Add("--location");
            startInfo.ArgumentList.Add(discordPath);

            using var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            var successDetected = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var processExited = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            process.OutputDataReceived += (_, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.Data))
                {
                    return;
                }

                lock (outputLines)
                {
                    outputLines.Add(args.Data);
                }

                progress?.Report(args.Data);

                if (args.Data.Contains(
                        "Success!",
                        StringComparison.OrdinalIgnoreCase) ||
                    args.Data.Contains(
                        "Successfully patched",
                        StringComparison.OrdinalIgnoreCase))
                {
                    successDetected.TrySetResult(true);
                }
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.Data))
                {
                    return;
                }

                lock (errorLines)
                {
                    errorLines.Add(args.Data);
                }

                progress?.Report(args.Data);
            };

            process.Exited += (_, _) =>
            {
                processExited.TrySetResult(true);
            };

            if (!process.Start())
            {
                return new VencordRepairResult(
                    false,
                    "Failed to start VencordInstallerCli.exe.",
                    string.Empty,
                    string.Empty);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

            timeoutCts.CancelAfter(RepairTimeout);

            var completedTask = await Task.WhenAny(
                successDetected.Task,
                processExited.Task);

            if (completedTask == successDetected.Task)
            {
                progress?.Report(
                    "Vencord repair completed successfully.");

                return new VencordRepairResult(
                    true,
                    "Vencord repair completed successfully.",
                    GetOutput(outputLines),
                    GetOutput(errorLines));
            }

            await processExited.Task.WaitAsync(
                timeoutCts.Token);

            if (process.ExitCode == 0)
            {
                progress?.Report(
                    "Vencord repair completed successfully.");

                return new VencordRepairResult(
                    true,
                    "Vencord repair completed successfully.",
                    GetOutput(outputLines),
                    GetOutput(errorLines));
            }

            return new VencordRepairResult(
                false,
                $"Vencord installer exited with code {process.ExitCode}.",
                GetOutput(outputLines),
                GetOutput(errorLines));
        }
        catch (OperationCanceledException)
        {
            return new VencordRepairResult(
                false,
                "Vencord repair timed out or was cancelled.",
                GetOutput(outputLines),
                GetOutput(errorLines));
        }
        catch (Exception ex)
        {
            return new VencordRepairResult(
                false,
                ex.ToString(),
                GetOutput(outputLines),
                GetOutput(errorLines));
        }
    }

    private static string GetOutput(
        List<string> lines)
    {
        lock (lines)
        {
            return string.Join(
                Environment.NewLine,
                lines);
        }
    }
}

public sealed record VencordRepairResult(
    bool Success,
    string Message,
    string Output,
    string Error);