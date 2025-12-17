# Oabac - Application Design & Requirements Document (DRD)

This document serves as a comprehensive blueprint for replicating the Oabac application. It details the UI design, functional requirements, and specific behaviors of every control and feature.

## 1. Technology Stack & Architecture
- **Framework**: .NET 8 Desktop Runtime
- **UI Framework**: WinUI 3 (Windows App SDK)
- **Language**: C#
- **Packaging**: MSIX (Single-project MSIX packaging recommended)
- **Design System**: Fluent Design (Windows 11 style)

## 2. Global UI Design
- **Window Style**:
  - Custom Title Bar (extends content into title bar).
  - **Backdrop**: `MicaBackdrop` (Windows 11 material).
  - **Dimensions**: Fixed size or min-size (approx. 850x680).
  - **Maximize**: Disabled (Fixed window utility style).
- **Navigation**:
  - `NavigationView` control with `PaneDisplayMode="Left"`.
  - **Menu Items**: Home, Mappings, Activity Log.
  - **Settings**: Built-in Settings item at the bottom.
- **Theme**:
  - Dark/Light theme aware (uses `ThemeResource` brushes).
  - Card-based layout for content sections (`LayerFillColorDefaultBrush` background, rounded corners).

## 3. Navigation Structure & Pages

### 3.1. Main Window Shell (`MainWindow.xaml`)
- **Title Bar**: Custom implementation containing the App Icon (16x16) and Title "Oabac".
- **Navigation Pane**:
  - **Home** (Icon: Home)
  - **Mappings** (Icon: Sync)
  - **Activity Log** (Icon: Document)
  - **Settings** (Standard Settings Item)

---

### 3.2. Home Page / Dashboard (`HomePage.xaml`)
**Purpose**: Quick status overview and manual control.

**UI Elements:**
1.  **Header**: Text "Dashboard" (Title Style).
2.  **Sync Control Card**:
    -   **Button**: "Sync Now"
        -   **Icon**: Sync glyph (`&#xE895;`).
        -   **Style**: `AccentButtonStyle` (Primary color).
        -   **Action**: Triggers a manual execution of the sync logic immediately in the background.
3.  **Configured Folders Card**:
    -   **Header**: "CONFIGURED SYNC FOLDERS".
    -   **List**: Read-only view of current mappings.
        -   **Item Template**: Shows Source Path (Bold) and Destination Path (Secondary text with arrow icon `&#xE72A;`).
    -   **Empty State**: Text "No sync mappings configured..." visible when list is empty.

---

### 3.3. Mappings Page (`MappingsPage.xaml`)
**Purpose**: Manage source and destination folder pairs.

**UI Elements:**
1.  **Existing Mappings Card**:
    -   **List Control**: `ListView` displaying active mappings.
    -   **Selection**: Single selection mode (to choose item for removal).
2.  **Add/Remove Card**:
    -   **Input Field 1**: "Source Folder"
        -   Control: `TextBox` (Header: "Source Folder").
        -   **Button**: "Browse..." (Triggers `FolderPicker`).
    -   **Input Field 2**: "Destination Folder"
        -   Control: `TextBox` (Header: "Destination Folder").
        -   **Button**: "Browse..." (Triggers `FolderPicker`).
    -   **Action Buttons**:
        -   **Button**: "Add Mapping"
            -   **Style**: `AccentButtonStyle`.
            -   **Logic**: Validates non-empty paths, checks for duplicates, saves to settings, and re-initializes the Sync Service.
        -   **Button**: "Remove Selected"
            -   **Logic**: Removes currently selected item from the list and settings, then re-initializes the Sync Service.

---

### 3.4. Settings Page (`SettingsPage.xaml`)
**Purpose**: Global configuration of application behavior.

**UI Elements:**
1.  **Sync Settings Card**:
    -   **Option 1**: "Sync Mode"
        -   **Control**: `ComboBox`.
        -   **Values**:
            -   "Interval": Syncs on a timer.
            -   "Realtime": Uses `FileSystemWatcher` to sync immediately on file changes.
    -   **Option 2**: "Sync Interval"
        -   **Control**: `ComboBox`.
        -   **Values**: "30 min", "1 hr", "2 hr".
        -   **Behavior**: Disabled if Sync Mode is "Realtime".
    -   **Option 3**: "Mirror Deletions"
        -   **Control**: `ToggleSwitch`.
        -   **Description**: "Delete files from destination when they're removed from source."
        -   **Logic**: If ON, files present in Destination but missing in Source are deleted. If OFF, Destination accumulates files (additive only).

2.  **Application Settings Card**:
    -   **Option 4**: "Run at Startup"
        -   **Control**: `ToggleSwitch`.
        -   **Logic**: Modifies Registry (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) to launch app with `--background` flag.
    -   **Option 5**: "Minimize to Tray"
        -   **Control**: `ToggleSwitch`.
        -   **Logic**: If ON, closing the window (`X` button) hides the window instead of terminating the process.

---

### 3.5. Activity Log Page (`ActivityPage.xaml`)
**Purpose**: View runtime logs for debugging and verification.

**UI Elements:**
1.  **Log Display**:
    -   **Control**: `TextBox` (ReadOnly, Multiline, TextWrapping).
    -   **Behavior**: Appends new log lines to the top.
    -   **Limit**: Truncates text if it exceeds 10,000 characters to prevent memory issues.
    -   **Events**: Subscribes to `SyncService.Status` event on Load, Unsubscribes on Unload.

---

## 4. System Tray Integration (`TrayIconService.cs`)
- **Icon**: Uses `Assets/sync.ico`. Falls back to System Application Icon if missing.
- **Behavior**:
    -   **Left Double-Click**: Opens/Restores the Main Window.
    -   **Right-Click**: Opens Context Menu.
- **Context Menu Options**:
    1.  **Open**: Restores Main Window.
    2.  **Sync Now**: Triggers manual sync.
    3.  **Exit**: Fully terminates the application (disposes tray icon and services).

## 5. Functional Logic & Backend

### 5.1. Sync Service (`SyncService.cs`)
- **Singleton**: One instance shared across the app (`App.SyncService`).
- **Configuration**: Reloads whenever settings change.
- **Modes**:
    -   **Interval**: Uses `System.Threading.Timer`.
    -   **Realtime**: Uses `FileSystemWatcher` (Filters: LastWrite, FileName, DirectoryName).
- **Sync Logic (One-Way Source -> Dest)**:
    1.  Iterate Source files.
    2.  Compare with Destination file.
    3.  **Copy Condition**: If Destination file is missing OR Source file is newer (`LastWriteTimeUtc`).
    4.  **Mirror Logic**: If enabled, iterate Destination files. If file not found in Source, delete from Destination.
- **Error Handling**:
    -   Must wrap file operations in `try-catch` to prevent crashes on locked files.
    -   Must validate directory existence before operations.

### 5.2. Startup Logic (`App.xaml.cs`)
- **Arguments**: Checks for `--background` argument.
- **Behavior**:
    -   If `--background` is present: Initialize Sync Service and Tray Icon, but do **not** show Main Window.
    -   If normal launch: Show Main Window.

### 5.3. Robustness Features
- **Global Exception Handling**:
    -   `Application.UnhandledException` (UI Thread).
    -   `AppDomain.UnhandledException` (Background Threads).
    -   `TaskScheduler.UnobservedTaskException` (Async Tasks).
- **P/Invoke Safety**:
    -   Use `GetWindowLongPtr` (64-bit safe) for window style modification.
    -   Check for `IntPtr.Zero` handles before Win32 API calls.


### 5.4. Performance & Reliability Standards:
- **Non-Blocking Operations: 
    -  Ensures the UI remains responsive during large file transfers.
- **Robustness ("NASA-style" Reliability): 
    -  Mandates fault tolerance (skipping errors instead of crashing), state recovery, strict resource management, and defensive programming for all external interactions.