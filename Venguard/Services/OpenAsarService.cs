using System.Diagnostics;
using System.IO;

namespace Venguard.Services;

public sealed class OpenAsarService
{
    private static readonly TimeSpan OperationTimeout =
        TimeSpan.FromMinutes(2);

    public async Task<OpenAsarResult> SetEnabledAsync(
        string installerPath,
        string discordPath,
        bool enabled,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(installerPath))
        {
            return new OpenAsarResult(
                false,
                "Vencord installer was not found.",
                string.Empty,
                string.Empty);
        }

        if (!Directory.Exists(discordPath))
        {
            return new OpenAsarResult(
                false,
                "Discord installation was not found.",
                string.Empty,
                string.Empty);
        }

        var outputLines = new List<string>();
        var errorLines = new List<string>();

        try
        {
            progress?.Report(
                enabled
                    ? "Installing OpenAsar..."
                    : "Removing OpenAsar...");

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
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            var successDetected =
                new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            var processExited =
                new TaskCompletionSource<bool>(
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
                return new OpenAsarResult(
                    false,
                    "Failed to start VencordInstallerCli.exe.",
                    string.Empty,
                    string.Empty);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            timeoutCts.CancelAfter(OperationTimeout);

            var completedTask = await Task.WhenAny(
                successDetected.Task,
                processExited.Task);

            if (completedTask == successDetected.Task)
            {
                progress?.Report(
                    enabled
                        ? "OpenAsar installed. Closing installer..."
                        : "OpenAsar removed. Closing installer...");

                await StopProcessAsync(
                    process,
                    timeoutCts.Token);

                return new OpenAsarResult(
                    true,
                    enabled
                        ? "OpenAsar installed successfully."
                        : "OpenAsar removed successfully.",
                    GetOutput(outputLines),
                    GetOutput(errorLines));
            }

            if (!processExited.Task.IsCompleted)
            {
                await processExited.Task.WaitAsync(
                    timeoutCts.Token);
            }

            if (process.ExitCode == 0)
            {
                return new OpenAsarResult(
                    true,
                    enabled
                        ? "OpenAsar installed successfully."
                        : "OpenAsar removed successfully.",
                    GetOutput(outputLines),
                    GetOutput(errorLines));
            }

            return new OpenAsarResult(
                false,
                enabled
                    ? $"OpenAsar installer exited with code {process.ExitCode}."
                    : $"OpenAsar uninstaller exited with code {process.ExitCode}.",
                GetOutput(outputLines),
                GetOutput(errorLines));
        }
        catch (OperationCanceledException)
        {
            return new OpenAsarResult(
                false,
                "OpenAsar operation timed out or was cancelled.",
                GetOutput(outputLines),
                GetOutput(errorLines));
        }
        catch (Exception ex)
        {
            return new OpenAsarResult(
                false,
                ex.ToString(),
                GetOutput(outputLines),
                GetOutput(errorLines));
        }
    }

    private static async Task StopProcessAsync(
        Process process,
        CancellationToken cancellationToken)
    {
        if (process.HasExited)
        {
            return;
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        try
        {
            await process.WaitForExitAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
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

public sealed record OpenAsarResult(
    bool Success,
    string Message,
    string Output,
    string Error);