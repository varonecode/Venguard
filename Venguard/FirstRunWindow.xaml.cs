using System.Windows;
using System.Windows.Input;
using Venguard.Config;

namespace Venguard;

public partial class FirstRunWindow : Window
{
    public VenguardConfig Config { get; }

    public FirstRunWindow(VenguardConfig config)
    {
        Config = config;

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

    private void ContinueButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Config.AutoStart = AutoStartCheckBox.IsChecked == true;
        Config.LaunchDiscordAfterPatch = LaunchDiscordCheckBox.IsChecked == true;
        Config.UseOpenAsar = OpenAsarCheckBox.IsChecked == true;

        DialogResult = true;
    }
}