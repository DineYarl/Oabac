# Oabac

**Oabac** is a modern, lightweight, and powerful folder synchronization tool for Windows, built with **WinUI 3** and **.NET 8**. It helps you keep your files backed up and synchronized across different locations with ease.

## Features

*   **Flexible Sync Modes**:
    *   **Real-time**: Instantly sync changes as they happen.
    *   **Interval**: Schedule syncs to run every 30 minutes, 1 hour, etc.
*   **Sync Directions**:
    *   **One-Way**: Mirror source to destination.
    *   **Two-Way**: Keep both folders in sync (merge changes).
*   **Smart Filtering**: Exclude files and folders using patterns (e.g., `*.tmp`, `node_modules`).
*   **Safety First**:
    *   **Recycle Bin Support**: Deleted or overwritten files can be sent to the Recycle Bin instead of being permanently lost.
    *   **Mirror Deletions**: Optional setting to propagate deletions from source to destination.
*   **System Integration**:
    *   **Minimize to Tray**: Keep the app running in the background without cluttering your taskbar.
    *   **Run at Startup**: Automatically start syncing when you log in.
    *   **Toast Notifications**: Get notified when syncs complete or if errors occur.
*   **Modern UI**: Clean and responsive interface designed for Windows 11/10.

## Installation

### Option 1: Installer (Recommended)
1.  Go to the [Releases](https://github.com/DineYarl/Oabac/releases) page.
2.  Download **`Oabac_Installer.zip`**.
3.  Extract the zip file.
4.  Right-click **`Install.ps1`** and select **Run with PowerShell**.
    *   This will automatically install the required certificate and the app.

### Option 2: Portable (No Install)
1.  Go to the [Releases](https://github.com/DineYarl/Oabac/releases) page.
2.  Download the zip file matching your system architecture:
    *   **x64** (Standard 64-bit Windows): `Oabac_x64.zip`
    *   **x86** (32-bit Windows / Older PCs): `Oabac_x86.zip`
    *   **ARM64** (Surface Pro X / Snapdragon Laptops): `Oabac_arm64.zip`
3.  Extract the zip file to a folder of your choice.
4.  Run `Oabac.exe`.

> **Note**: Since this is a portable app, you might see a "Windows protected your PC" warning (SmartScreen). Click **More info** -> **Run anyway**.

## Building from Source

**Requirements**:
*   Visual Studio 2022 (17.8 or later)
*   .NET 8 SDK
*   Windows App SDK

**Steps**:
1.  Clone the repository.
2.  Open `Oabac.sln` in Visual Studio.
3.  Restore NuGet packages.
4.  Build and Run (F5).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
