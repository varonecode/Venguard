# Venguard

Venguard is a lightweight Windows utility for keeping a Discord installation healthy.

It monitors Discord for common problems, helps repair Vencord-related issues, and gives you control over when checks happen, what gets repaired, and how Venguard behaves in the background.

> **Windows only · .NET 8 · WPF**

---

## Features

* **Discord monitoring**

  * Periodically checks your Discord installation for problems.
  * Configurable check interval.
  * Optional background monitoring.
  * Can keep running in the system tray.

* **Vencord repair**

  * Detects problems with a Discord/Vencord installation.
  * Runs the repair process through the Vencord installer.
  * Optional confirmation before repairs.
  * Optional automatic Discord launch after a successful repair.

* **OpenAsar support**

  * Enable or disable OpenAsar from the settings window.
  * Uses the Vencord installer for the installation/removal process.
  * Checks that Discord is closed before changing the installation.

* **System tray integration**

  * Run Venguard without keeping the main window open.
  * Minimize to tray.
  * Close to tray.
  * Access important actions without reopening the main window.

* **Windows startup**

  * Start Venguard automatically with Windows.
  * Optionally start minimized.

* **Notifications**

  * Enable or disable Venguard notifications.
  * Separate notifications for successful and failed repairs.

* **First-run setup**

  * Simple first-run experience for configuring Venguard.

---

## Screenshots

Screenshots can be added here as the UI develops.

```text
screenshots/
├── main-window.png
├── settings.png
├── tray.png
└── first-run.png
```

---

## Requirements

### Operating system

* Windows 10 version 19041 or newer
* Windows 11

### Runtime

The project targets:

```text
.NET 8
```

The application uses WPF and targets:

```text
net8.0-windows10.0.19041.0
```

A compatible .NET 8 installation is required when running the project directly.

---

## Installation

### Download a release

The easiest way to use Venguard is to download the latest release from the project's Releases page.

Extract the application and run:

```text
Venguard.exe
```

No separate Discord installation or configuration file needs to be created manually.

### Build from source

Clone the repository:

```powershell
git clone https://github.com/yourusername/Venguard.git
cd Venguard
```

Build the project:

```powershell
dotnet build .\Venguard\Venguard.csproj
```

Run it with:

```powershell
dotnet run --project .\Venguard\Venguard.csproj
```

The executable will be located under:

```text
Venguard\bin\Debug\net8.0-windows10.0.19041.0\
```

---

## Project structure

```text
Venguard/
│
├── Venguard/
│   ├── Assets/
│   │   ├── Venguard.ico
│   │   └── VenguardLogo.png
│   │
│   ├── Config/
│   │   └── VenguardConfig.cs
│   │
│   ├── Resources/
│   │
│   ├── Services/
│   │   ├── DiscordMonitor.cs
│   │   ├── DiscordService.cs
│   │   ├── OpenAsarService.cs
│   │   ├── VencordInstallerManager.cs
│   │   └── VencordInstallerService.cs
│   │
│   ├── App.xaml
│   ├── App.xaml.cs
│   │
│   ├── FirstRunWindow.xaml
│   ├── FirstRunWindow.xaml.cs
│   │
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   │
│   ├── SettingsWindow.xaml
│   ├── SettingsWindow.xaml.cs
│   │
│   ├── Venguard.csproj
│   └── AssemblyInfo.cs
│
└── README.md
```

---

## Settings

Venguard exposes most of its behaviour through the Settings window.

### General

| Setting                     | Description                                                        |
| --------------------------- | ------------------------------------------------------------------ |
| Start Venguard with Windows | Launches Venguard automatically when Windows starts                |
| Start Venguard minimized    | Starts without opening the main window                             |
| Minimize to tray            | Keeps Venguard running when minimized                              |
| Close to tray               | Keeps Venguard running when the window is closed                   |
| Background monitoring       | Allows Venguard to monitor Discord while running in the background |

### Monitoring

The monitoring interval can be configured to suit your setup.

Available intervals include:

```text
5 seconds
10 seconds
15 seconds
30 seconds
1 minute
2 minutes
5 minutes
```

Background monitoring can also be disabled entirely.

When disabled, Venguard will not continuously monitor Discord in the background.

### Repair

You can control what happens after a repair:

* Launch Discord after a successful repair
* Ask for confirmation before repairing
* Use OpenAsar when available

### Notifications

Notifications can be controlled globally, with separate settings for:

* Successful repairs
* Failed repairs

---

## How Venguard works

Venguard is designed around a small number of services rather than putting all of the application logic inside the UI.

A simplified flow looks like this:

```text
                 ┌──────────────────┐
                 │     Venguard     │
                 └────────┬─────────┘
                          │
                          ▼
                 ┌──────────────────┐
                 │ Discord Monitor  │
                 └────────┬─────────┘
                          │
                    Problem found?
                     ┌────┴────┐
                    No         Yes
                    │           │
                    ▼           ▼
                 Continue    Repair
                                │
                                ▼
                     ┌──────────────────┐
                     │ Vencord Installer│
                     └────────┬─────────┘
                              │
                              ▼
                         Verification
                              │
                    ┌─────────┴─────────┐
                    ▼                   ▼
                 Success              Failure
                    │                   │
                    ▼                   ▼
               Notification        Notification
                    │
                    ▼
             Launch Discord
```

The exact repair process depends on the detected state of the Discord installation.

---

## System tray

Venguard can run without keeping its main window open.

Depending on your settings:

* Minimizing the window can send Venguard to the tray.
* Closing the window can send Venguard to the tray.
* Venguard can continue monitoring while hidden.
* The tray icon can be used to access the application again.

This makes it possible to leave Venguard running without having another window taking up space on your desktop.

---

## Configuration

Venguard stores its configuration using the `VenguardConfig` model.

Current configuration options include:

```csharp
public bool IsFirstRun { get; set; }

public bool AutoStart { get; set; }

public bool StartMinimized { get; set; }

public bool MinimizeToTray { get; set; }

public bool CloseToTray { get; set; }

public bool LaunchDiscordAfterPatch { get; set; }

public bool UseOpenAsar { get; set; }

public bool ConfirmBeforeRepair { get; set; }

public bool EnableNotifications { get; set; }

public bool NotifyOnRepairSuccess { get; set; }

public bool NotifyOnRepairFailure { get; set; }

public int MonitorIntervalSeconds { get; set; }
```

The defaults are intended to provide a sensible first-run experience while still allowing advanced users to change Venguard's behaviour.

---

## Dependencies

Venguard currently uses:

* **WPF** for the desktop interface
* **.NET 8** as the application framework
* **Hardcodet.NotifyIcon.Wpf** for system tray functionality
* **Windowstoastapi** for Windows notifications
* **Vencord's installer** for supported installation and repair operations

NuGet dependencies are defined in:

```text
Venguard/Venguard.csproj
```

---

## Development

### Clone the repository

```powershell
git clone https://github.com/yourusername/Venguard.git
cd Venguard
```

### Restore dependencies

```powershell
dotnet restore .\Venguard\Venguard.csproj
```

### Build

```powershell
dotnet build .\Venguard\Venguard.csproj
```

### Run

```powershell
dotnet run --project .\Venguard\Venguard.csproj
```

### Clean the project

If stale WPF build files cause unexpected behaviour:

```powershell
dotnet clean .\Venguard\Venguard.csproj
```

You can also remove the generated build directories:

```powershell
Remove-Item .\Venguard\bin -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\Venguard\obj -Recurse -Force -ErrorAction SilentlyContinue
```

Then rebuild:

```powershell
dotnet build .\Venguard\Venguard.csproj
```

---

## Why WPF?

Venguard is a Windows-only application, so WPF provides a good fit for the project.

It gives the application:

* Native Windows integration
* System tray support
* Windows notification support
* Flexible XAML-based UI
* Straightforward .NET integration
* Good support for custom window chrome and themes

The UI intentionally avoids looking like a default WPF application and uses a dark, modern interface.

---

## Design goals

Venguard is intended to stay:

### Simple

The application should be understandable without reading documentation.

### Quiet

When everything is working, Venguard should stay out of the way.

### Safe

Repairs should avoid making unnecessary changes to a user's Discord installation.

### Transparent

Important actions should be visible and understandable rather than hidden behind unexplained automation.

### Lightweight

Monitoring should not require a large application footprint or unnecessary background activity.

---

## Roadmap

Potential future improvements include:

* [ ] More detailed Discord health checks
* [ ] Repair history
* [ ] Repair logs
* [ ] Exportable diagnostic reports
* [ ] Improved tray menu
* [ ] More granular monitoring controls
* [ ] Automatic detection of Discord installation changes
* [ ] Multiple Discord installation support
* [ ] Better repair progress information
* [ ] More detailed error reporting
* [ ] Optional scheduled health checks
* [ ] Portable mode
* [ ] Automatic update support
* [ ] Improved accessibility
* [ ] More UI customization
* [ ] Installer/package distribution
* [ ] Release builds and signed binaries

The roadmap is intentionally flexible. Features should only be added when they provide a meaningful improvement to the application.

---

## Troubleshooting

### The application builds but an old UI appears

Make sure you're running the project you just built:

```powershell
dotnet run --project .\Venguard\Venguard.csproj
```

If necessary, clean the project first:

```powershell
dotnet clean .\Venguard\Venguard.csproj

Remove-Item .\Venguard\bin -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\Venguard\obj -Recurse -Force -ErrorAction SilentlyContinue

dotnet build .\Venguard\Venguard.csproj
dotnet run --project .\Venguard\Venguard.csproj
```

### The settings window does not reflect XAML changes

Verify that the XAML file exists and is not empty:

```powershell
Get-Item .\Venguard\SettingsWindow.xaml |
    Select-Object FullName, Length, LastWriteTime
```

You should see a non-zero file size.

You can also verify that the expected XAML is present:

```powershell
Select-String `
    -Path .\Venguard\SettingsWindow.xaml `
    -Pattern "IntervalComboBox"
```

### The project says a file does not exist

The project file is located at:

```text
Venguard\Venguard.csproj
```

From the repository root, use:

```powershell
dotnet run --project .\Venguard\Venguard.csproj
```

Do not add a trailing `\` to the `.csproj` path.

Correct:

```powershell
dotnet run --project .\Venguard\Venguard.csproj
```

Incorrect:

```powershell
dotnet run --project .\Venguard\Venguard.csproj\
```

### XAML reports that a resource cannot be found

WPF resources are case-sensitive.

For example:

```text
VenguardLogoStyle
```

is different from:

```text
VenguardLogostyle
```

Check that the resource exists and that the key matches exactly.

---

## Contributing

Contributions are welcome.

Before opening a pull request:

1. Build the project.
2. Make sure the application starts.
3. Test the affected feature manually.
4. Check that existing settings still work.
5. Keep unrelated changes out of the pull request.

For larger changes, opening an issue first is recommended so the approach can be discussed before implementation.

---

## Code style

Venguard follows standard C# conventions where practical.

In particular:

* Nullable reference types are enabled.
* Classes and public members use clear names.
* UI code stays in the WPF layer where possible.
* Application logic is separated into services.
* Configuration is represented by a dedicated model.
* Exceptions should be handled at appropriate application boundaries.

The goal is maintainable code rather than excessive abstraction.

---

## Security and privacy

Venguard is intended to operate locally on the user's Windows machine.

The application interacts with local Discord files and processes as required for its monitoring and repair functionality.

Users should review the source code before building or running modified versions of the application.

Do not run unofficial builds from untrusted sources with administrator privileges unless you understand what they do.

---

## Disclaimer

Venguard is an independent project.

It is not affiliated with, endorsed by, or sponsored by Discord or Vencord unless explicitly stated by the project maintainers.

Discord and Vencord are trademarks of their respective owners.

---

## License

Add the project's license here.

For example:

```text
MIT License
```

If the project is released under the MIT License, include the full `LICENSE` file in the repository.

---

## Acknowledgements

Venguard would not exist without the work of the projects and communities it builds around.

Thanks to:

* The **Vencord** project and its contributors
* The **Discord** community
* The **.NET** and **WPF** communities
* The developers of **Hardcodet.NotifyIcon.Wpf**
* The developers of the Windows notification libraries used by the project

---

## Support

If you find a bug, please open an issue and include:

* Windows version
* Venguard version/commit
* What you expected to happen
* What actually happened
* Relevant error messages
* Steps to reproduce the issue

For example:

```text
Windows: Windows 11 24H2
Venguard: 0.x.x

Steps:
1. Open Venguard
2. Open Settings
3. Enable background monitoring
4. Save settings
5. Restart Venguard

Expected:
Background monitoring remains enabled.

Actual:
Background monitoring is disabled after restart.
```

Avoid posting personal information, Discord account information, authentication tokens, or complete private logs containing sensitive data.

---

## Project status

Venguard is an actively developed project.

Features and configuration options may change as development continues, and some parts of the application may still be experimental.

The main priority is to make Discord maintenance **simple, reliable, and unobtrusive** without turning Venguard into another complicated system utility.

---

<p align="center">
  <strong>Venguard</strong><br>
  Keep Discord healthy. Stay out of the way.
</p>
