using System.Windows;
using System.Windows.Input;
using Venguard.Config;

namespace Venguard;

public partial class SettingsWindow : Window
{
    private readonly VenguardConfig _config;

    public SettingsWindow(VenguardConfig config)
    {
        _config = config;

        InitializeComponent();

        AutoStartCheckBox.IsChecked = config.AutoStart;
        LaunchDiscordCheckBox.IsChecked = config.LaunchDiscordAfterPatch;
        OpenAsarCheckBox.IsChecked = config.UseOpenAsar;
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

    private void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _config.AutoStart =
            AutoStartCheckBox.IsChecked == true;

        _config.LaunchDiscordAfterPatch =
            LaunchDiscordCheckBox.IsChecked == true;

        _config.UseOpenAsar =
            OpenAsarCheckBox.IsChecked == true;

        DialogResult = true;
    }
}