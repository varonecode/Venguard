using System.Windows;
using System.Windows.Input;
using Venguard.Config;

namespace Venguard;

public partial class FirstRunWindow : Window
{
    private readonly VenguardConfig _config;

    public FirstRunWindow(
        VenguardConfig config)
    {
        _config = config;

        InitializeComponent();

        AutoStartCheckBox.IsChecked =
            _config.AutoStart;

        StartMinimizedCheckBox.IsChecked =
            _config.StartMinimized;

        LaunchDiscordCheckBox.IsChecked =
            _config.LaunchDiscordAfterPatch;

        UseOpenAsarCheckBox.IsChecked =
            _config.UseOpenAsar;
    }

    private void TitleBar_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void ContinueButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _config.AutoStart =
            AutoStartCheckBox.IsChecked == true;

        _config.StartMinimized =
            StartMinimizedCheckBox.IsChecked == true;

        _config.LaunchDiscordAfterPatch =
            LaunchDiscordCheckBox.IsChecked == true;

        _config.UseOpenAsar =
            UseOpenAsarCheckBox.IsChecked == true;

        DialogResult = true;
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }
}