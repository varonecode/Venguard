using System.Windows;
using System.Windows.Input;
using Venguard.Config;
using Venguard.Services;

namespace Venguard;

public partial class SettingsWindow : Window
{
    private readonly VenguardConfig _config;
    private readonly DiscordService _discordService;
    private readonly VencordInstallerManager _installerManager;
    private readonly OpenAsarService _openAsarService;

    public SettingsWindow(
        VenguardConfig config,
        DiscordService discordService,
        VencordInstallerManager installerManager,
        OpenAsarService openAsarService)
    {
        _config = config;
        _discordService = discordService;
        _installerManager = installerManager;
        _openAsarService = openAsarService;

        InitializeComponent();

        AutoStartCheckBox.IsChecked = config.AutoStart;
        LaunchDiscordCheckBox.IsChecked =
            config.LaunchDiscordAfterPatch;
        OpenAsarCheckBox.IsChecked =
            config.UseOpenAsar;
    }

    private void TitleBar_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private async void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var newAutoStart =
                AutoStartCheckBox.IsChecked == true;

            var newLaunchDiscord =
                LaunchDiscordCheckBox.IsChecked == true;

            var newUseOpenAsar =
                OpenAsarCheckBox.IsChecked == true;

            if (newUseOpenAsar != _config.UseOpenAsar)
            {
                var installation =
                    _discordService.GetInstallation();

                if (installation is null)
                {
                    MessageBox.Show(
                        "Discord Stable was not found, so OpenAsar could not be changed.",
                        "OpenAsar",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (_discordService.IsDiscordRunning())
                {
                    MessageBox.Show(
                        "Please fully close Discord from the system tray before changing the OpenAsar setting.",
                        "Discord is running",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var installerPath =
                    await _installerManager.GetInstallerAsync();

                var confirmation = MessageBox.Show(
                    newUseOpenAsar
                        ? "Venguard will install OpenAsar using the official Vencord installer. Continue?"
                        : "Venguard will uninstall OpenAsar using the official Vencord installer. Continue?",
                    newUseOpenAsar
                        ? "Install OpenAsar"
                        : "Uninstall OpenAsar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes)
                {
                    return;
                }

                var openAsarResult =
                    await _openAsarService.SetEnabledAsync(
                        installerPath,
                        installation.DiscordPath,
                        newUseOpenAsar);

                if (!openAsarResult.Success)
                {
                    var details =
                        string.IsNullOrWhiteSpace(
                            openAsarResult.Error)
                            ? openAsarResult.Output
                            : openAsarResult.Error;

                    MessageBox.Show(
                        $"{openAsarResult.Message}\n\n{details}",
                        "OpenAsar",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    return;
                }
            }

            _config.AutoStart = newAutoStart;

            _config.LaunchDiscordAfterPatch =
                newLaunchDiscord;

            _config.UseOpenAsar =
                newUseOpenAsar;

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Venguard Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}