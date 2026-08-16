using System.Diagnostics;

namespace Venguard.Services;

public sealed class DiscordLauncherService
{
    public void Launch()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "discord://discord.com/channels/@me",
            UseShellExecute = true
        });
    }
}