using System.IO;

namespace Venguard.Services;

public sealed class StartupService
{
    private const string StartupFileName = "Venguard.cmd";

    private readonly string _startupFolder;

    public StartupService()
    {
        _startupFolder = Environment.GetFolderPath(
            Environment.SpecialFolder.Startup);
    }

    public bool IsEnabled()
    {
        return File.Exists(GetStartupFilePath());
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            CreateStartupFile();
        }
        else
        {
            RemoveStartupFile();
        }
    }

    private void CreateStartupFile()
    {
        var executablePath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return;
        }

        Directory.CreateDirectory(_startupFolder);

        var startupFilePath = GetStartupFilePath();
        var workingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty;

        var content = $"@echo off\r\ncd /d \"{workingDirectory}\"\r\nstart \"\" \"{executablePath}\"\r\n";

        File.WriteAllText(startupFilePath, content);
    }

    private void RemoveStartupFile()
    {
        var startupFilePath = GetStartupFilePath();

        if (File.Exists(startupFilePath))
        {
            File.Delete(startupFilePath);
        }
    }

    private string GetStartupFilePath()
    {
        return Path.Combine(_startupFolder, StartupFileName);
    }
}