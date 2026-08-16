using System.IO;

namespace Venguard.Services;

public sealed class VencordInstallerManager
{
    private readonly string _installerDirectory;
    private readonly string _installerPath;
    private readonly VencordInstallerDownloader _downloader;

    public VencordInstallerManager(
        VencordInstallerDownloader downloader)
    {
        _downloader = downloader;

        _installerDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Venguard");

        _installerPath = Path.Combine(
            _installerDirectory,
            "VencordInstallerCli.exe");
    }

    public bool IsInstalled()
    {
        return File.Exists(_installerPath);
    }

    public async Task<string> GetInstallerAsync(
        CancellationToken cancellationToken = default)
    {
        if (IsInstalled())
        {
            return _installerPath;
        }

        Directory.CreateDirectory(_installerDirectory);

        var downloadedPath = await _downloader.DownloadAsync(
            _installerDirectory,
            cancellationToken);

        if (!string.Equals(
                downloadedPath,
                _installerPath,
                StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(downloadedPath, _installerPath, true);
            File.Delete(downloadedPath);
        }

        return _installerPath;
    }
}