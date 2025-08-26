# Oabac - Automatic Folder Synchronization

A modern Windows application for automatic folder synchronization built with WinUI 3 and .NET 8.

## Features

?? **Real-time Synchronization**
- Monitor folders for changes and sync automatically
- Interval-based sync with customizable timing
- Support for multiple folder mappings

?? **Modern UI**
- Native Windows 11 design with Mica backdrop
- Task Manager-style navigation
- Dark theme with accent colors
- Responsive and intuitive interface

?? **Advanced Settings**
- Mirror deletions option
- Run at Windows startup
- Minimize to system tray
- Configurable sync intervals (30 min, 1 hr, 2 hr)

## Screenshots

![Oabac Dashboard](screenshots/dashboard.png)
*Modern dashboard with sync controls*

## System Requirements

- Windows 10 version 1809 (build 17763) or later
- Windows 11 (recommended for best experience)
- .NET 8 Desktop Runtime

## Installation

### Method 1: Download Release
1. Go to [Releases](../../releases)
2. Download the latest `Oabac-Setup.exe`
3. Run the installer and follow the setup wizard

### Method 2: Manual Installation
1. Download the latest release ZIP file
2. Extract to a folder of your choice
3. Run `Oabac.exe`

## Usage

1. **Launch Oabac** from Start Menu or desktop shortcut
2. **Go to Mappings** page to add folder pairs
3. **Configure sync settings** in the Settings page
4. **Start syncing** from the Dashboard

### Adding Folder Mappings
1. Navigate to the "Mappings" page
2. Click "Browse..." to select source folder
3. Click "Browse..." to select destination folder
4. Click "Add Mapping" to save

### Sync Modes
- **Interval**: Sync at regular intervals (30 min, 1 hr, 2 hr)
- **Realtime**: Monitor and sync immediately when changes occur

## Building from Source

### Prerequisites
- Visual Studio 2022 with:
  - .NET desktop development workload
  - Windows App SDK
- Windows 10/11 SDK

### Build Steps
```bash
git clone https://github.com/yourusername/oabac.git
cd oabac
dotnet restore
dotnet build
```

### Running
```bash
dotnet run --project Oabac.csproj
```

## Technology Stack

- **Framework**: .NET 8
- **UI**: WinUI 3 with Windows App SDK
- **Design**: Fluent Design System
- **Packaging**: MSIX (Windows App Package)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup
1. Clone the repository
2. Open `Oabac.sln` in Visual Studio 2022
3. Restore NuGet packages
4. Build and run

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

If you encounter any issues or have questions:
- Create an [Issue](../../issues)
- Check existing [Discussions](../../discussions)

## Changelog

### Version 1.0.0
- Initial release
- Real-time and interval sync modes
- Modern WinUI 3 interface
- System tray integration
- Multiple folder mapping support

---

**Made with ?? using WinUI 3 and .NET 8**