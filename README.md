<img width="256" height="256" alt="Gemini_Generated_Image_i85xn6i85xn6i85x" src="https://github.com/user-attachments/assets/752544c1-f39f-40b3-be0b-84722669241c" />

# Venguard

Venguard is a Windows utility for maintaining Vencord installations. It monitors Discord, detects when Vencord has been corrupted or removed, and can automatically repair the installation using the official Vencord installer.

## Features

- **Background monitoring**: Periodically checks Discord and Vencord status with configurable intervals
- **Automatic repair**: Can run repairs automatically when problems are detected, or on-demand through the UI
- **OpenAsar management**: Optionally install or remove OpenAsar during repairs
- **System tray**: Minimizes to the system tray and can run in the background
- **Notifications**: Send toast notifications for repair events
- **Windows startup integration**: Optionally launch with Windows and start minimized
- **First-run setup**: Wizard guides initial configuration
- **Configurable behavior**: Extensive settings for notifications, auto-launch, tray behavior, and monitoring

## Requirements

- Windows 10 version 19041 or later
- .NET 8 Desktop Runtime

## Building

Clone the repository and build with .NET CLI:

```bash
dotnet build Venguard.csproj
```

Or with Visual Studio 2022+:

```bash
dotnet build Venguard.csproj
```

For a release build:

```bash
dotnet publish Venguard.csproj --configuration Release --self-contained
```

## Running

After building:

```bash
dotnet run --project Venguard.csproj
```

Or launch the compiled executable directly:

```bash
Venguard.exe
```

## Configuration

Venguard stores its configuration as JSON in the local application data directory. The first time you run it, a wizard will guide you through initial setup.

### Configuration Options

| Setting | Default | Purpose |
|---------|---------|---------|
| `AutoStart` | true | Start with Windows |
| `StartMinimized` | false | Launch in minimized state |
| `MinimizeToTray` | true | Minimize button hides to tray instead of taskbar |
| `CloseToTray` | true | Close button hides to tray instead of exiting |
| `LaunchDiscordAfterPatch` | true | Automatically launch Discord after repair succeeds |
| `UseOpenAsar` | true | Install/use OpenAsar during repair |
| `ConfirmBeforeRepair` | true | Show confirmation dialog before starting repairs |
| `EnableNotifications` | true | Send toast notifications |
| `NotifyOnRepairSuccess` | true | Notify when repairs complete successfully |
| `NotifyOnRepairFailure` | true | Notify when repairs fail |
| `EnableBackgroundMonitoring` | true | Continuously monitor Discord status |
| `MonitorIntervalSeconds` | 10 | Check Discord status every N seconds (minimum 5) |

## How it works

Venguard operates by continuously monitoring your Discord installation for signs that Vencord has been corrupted or removed. When a problem is detected, it can either notify you or automatically trigger a repair.

### Monitoring

The `DiscordMonitor` runs a timer at your configured interval and checks:

- Whether Discord is installed
- Whether Vencord's patch file exists (at `resources/_app.asar`)
- Whether OpenAsar is currently in use

When the status changes, it notifies the UI and can trigger background actions based on your settings.

### Repair Process

The `VencordRepairService` orchestrates repairs:

1. Locates the Discord installation (looks for `app-*` directories in `%LocalAppData%\Discord`)
2. Downloads the official Vencord installer (if needed)
3. Runs the installer with `--repair --location <discord-path>`
4. Conditionally installs or removes OpenAsar depending on settings
5. Optionally launches Discord when complete

The repair runs in a separate process and can be cancelled at any time.

## Project structure

```
Venguard/
├── Assets/
│   ├── Venguard.ico
│   └── VenguardLogo.png
│
├── Config/
│   ├── ConfigService.cs          # Loads/saves configuration to JSON
│   └── VenguardConfig.cs          # Configuration model
│
├── Services/
│   ├── DiscordInstallation.cs     # Represents a Discord installation path
│   ├── DiscordLauncherService.cs  # Launches Discord.exe
│   ├── DiscordMonitor.cs          # Timer-based status monitoring
│   ├── DiscordService.cs          # Detects Discord installation and status
│   ├── DiscordStatus.cs           # Status record (installed, patched, OpenAsar)
│   ├── NotificationService.cs     # Sends toast notifications
│   ├── OpenAsarService.cs         # Manages OpenAsar installation
│   ├── StartupService.cs          # Manages Windows startup registry entries
│   ├── VencordInstallerDownloader.cs    # Downloads installer from GitHub
│   ├── VencordInstallerManager.cs       # Caches and manages installer
│   ├── VencordInstallerService.cs       # Runs the installer process
│   └── VencordRepairService.cs          # Orchestrates the full repair flow
│
├── App.xaml              # Application resources and styling
├── App.xaml.cs           # Application startup and service initialization
├── FirstRunWindow.xaml   # First-run wizard UI
├── MainWindow.xaml       # Main application window
├── MainWindow.xaml.cs    # Main window logic and UI event handlers
└── Venguard.csproj       # Project file
```

### Key Services

**DiscordService**: Locates the Discord installation by looking in `%LocalAppData%\Discord` for version folders matching `app-*`. It checks the `resources` directory for both the original `app.asar` and Vencord's `_app.asar` patch file to determine if Vencord is installed. It also detects OpenAsar by reading the .asar files and checking for the "OpenAsar" string.

**DiscordMonitor**: Runs a timer at the configured interval and calls `DiscordService.GetStatus()` repeatedly. When the status changes, it raises the `StatusChanged` event, which the UI listens to for updates.

**VencordRepairService**: The main repair orchestrator. It gets the Discord installation, downloads the Vencord installer, runs it with the `--repair` flag, and then conditionally manages OpenAsar based on settings.

**VencordInstallerManager**: Caches the downloaded Vencord installer to avoid re-downloading it on every repair. The installer is fetched from the official Vencord GitHub releases.

**NotificationService**: Sends Windows toast notifications using the Windows Toast Notification API. The notifications appear in the system notification center.

**StartupService**: Manages the Windows registry entry in `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run` to enable/disable autostart.

## Development

The application uses:

- **WPF** for the UI with custom styling (no standard Windows theme)
- **Hardcodet.NotifyIcon.Wpf** for system tray functionality
- **Windowstoastapi** for toast notifications
- **Dependency injection**: Services are manually instantiated in `App.xaml.cs` and passed to windows

When adding new features, follow the existing pattern: create a service class in the `Services` folder, initialize it in `App.OnStartup()`, and inject it into the windows that need it.

## Troubleshooting

**Discord not found**: Venguard looks for Discord in `%LocalAppData%\Discord`. Ensure Discord is installed in the default location.

**Repair keeps failing**: Check that the official Vencord installer works standalone. Verify your Discord installation isn't corrupted beyond what Vencord can repair.

**Settings not saving**: Venguard stores configuration in the application data directory. Ensure you have write permissions to `%LocalAppData%\Venguard\`.

**Background monitoring not working**: Verify `EnableBackgroundMonitoring` is enabled in settings. Check that the monitoring interval is at least 5 seconds.

## Contributing

Contributions are welcome. When submitting changes:

- Keep the code style consistent with the existing project
- Add services for new functionality rather than adding to existing classes
- Test with an actual Discord/Vencord installation
- Update the configuration model in `VenguardConfig.cs` if adding new settings
- Update configuration loading/saving in `ConfigService.cs` if needed

## License

Venguard is licensed under the MIT License. See [LICENSE](LICENSE) for the full license text.
