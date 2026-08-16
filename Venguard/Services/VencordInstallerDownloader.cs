using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace Venguard.Services;

public sealed class VencordInstallerDownloader
{
    private const string InstallerUrl =
        "https://github.com/Vencord/Installer/releases/latest/download/VencordInstallerCli.exe";

    private const string ChecksumsUrl =
        "https://github.com/Vencord/Installer/releases/latest/download/checksums.sha256";

    private readonly HttpClient _httpClient;

    public VencordInstallerDownloader()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Venguard");
    }

    public async Task<string> DownloadAsync(
        string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDirectory);

        var installerPath = Path.Combine(
            destinationDirectory,
            "VencordInstallerCli.exe");

        var temporaryPath = installerPath + ".download";
        var checksumsPath = Path.Combine(
            destinationDirectory,
            "checksums.sha256");

        try
        {
            var installerBytes = await _httpClient.GetByteArrayAsync(
                InstallerUrl,
                cancellationToken);

            await File.WriteAllBytesAsync(
                temporaryPath,
                installerBytes,
                cancellationToken);

            await using var checksumStream = await _httpClient.GetStreamAsync(
                ChecksumsUrl,
                cancellationToken);

            await using var checksumFile = File.Create(checksumsPath);
            await checksumStream.CopyToAsync(
                checksumFile,
                cancellationToken);

            var expectedHash = await GetExpectedHashAsync(
                checksumsPath,
                cancellationToken);

            var actualHash = await ComputeHashAsync(
                temporaryPath,
                cancellationToken);

            if (!string.Equals(
                    expectedHash,
                    actualHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "The downloaded Vencord installer failed SHA-256 verification.");
            }

            File.Move(temporaryPath, installerPath, true);

            return installerPath;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            if (File.Exists(checksumsPath))
            {
                File.Delete(checksumsPath);
            }
        }
    }

    private static async Task<string> GetExpectedHashAsync(
        string checksumsPath,
        CancellationToken cancellationToken)
    {
        var lines = await File.ReadAllLinesAsync(
            checksumsPath,
            cancellationToken);

        var line = lines.FirstOrDefault(line =>
            line.Contains(
                "VencordInstallerCli.exe",
                StringComparison.OrdinalIgnoreCase));

        if (line is null)
        {
            throw new InvalidDataException(
                "The Vencord installer checksum was not found.");
        }

        var hash = line.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries)[0];

        if (hash.Length != 64)
        {
            throw new InvalidDataException(
                "The Vencord installer checksum is invalid.");
        }

        return hash;
    }

    private static async Task<string> ComputeHashAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);

        var hash = await SHA256.HashDataAsync(
            stream,
            cancellationToken);

        return Convert.ToHexString(hash);
    }
}