# ScreenHopper

A fast, Windows-first desktop utility to **teleport app windows across monitors** with precision zone snapping, per-app behavior profiles, and silent CLI automation.

ScreenHopper is built with **WPF + MVVM** on **.NET 10** and designed for both interactive users and power users who script window movement workflows.

---

## Why ScreenHopper

Modern multi-monitor setups are great—until window placement becomes repetitive and annoying.

ScreenHopper solves that by giving you:

- One-click monitor moves
- Smart zone snapping (halves, corners, full/center)
- Per-application remembered preferences
- Optional center double-click prep behavior
- Topmost + opacity controls
- Silent command-line mode for automation

---

## Core Features

### GUI workflow

- **Target Application picker** (visible GUI apps only)
- **Refresh Apps** to re-scan running windows
- **Destination Monitor picker**
- **Refresh Monitors** to re-enumerate displays
- **Window Modifiers**
  - Double-click center before moving
  - Always on Top
  - Opacity slider (0–100%)
- **Zone Snapping**
  - Top-Left, Top-Right
  - Bottom-Left, Bottom-Right
  - Left Half, Right Half
  - Full Screen / Center
- **Move Window Now** action

### App list intelligence

The app list excludes non-primary/non-user-facing entries and includes only real GUI windows.

Each item also supports:

- **Context menu: Hide from list**
  - Adds process to blacklist
  - Persists immediately
  - Refreshes list

### Persistent preferences (`preferences.json`)

Saved automatically using `System.Text.Json`:

- `BlacklistedProcesses`: list of hidden process names
- `AppPreferences`: per-process settings:
  - `RequiresDoubleClick`
  - `OpacityLevel`
  - `AlwaysOnTop`
  - `DefaultZone`

### Silent CLI execution

If startup arguments are provided, ScreenHopper:

1. Parses arguments
2. Executes moving logic without showing UI
3. Terminates automatically

---

## CLI Usage

```powershell
ScreenHopper.exe --move "chrome" --monitor 2 --zone "LeftHalf"
```

### Arguments

- `--move <processOrTitleText>`
  - Required for CLI mode
  - Matches process name or displayed app text
- `--monitor <index>`
  - 1-based monitor index
- `--zone <zoneName>`
  - Accepted values:
    - `LeftHalf`
    - `RightHalf`
    - `TopLeft`
    - `TopRight`
    - `BottomLeft`
    - `BottomRight`
    - `FullCenter`

If arguments are missing/invalid, GUI mode is used as fallback.

---

## Under the Hood

ScreenHopper uses native Win32 APIs through a centralized helper class:

- Foreground activation and restore
- Optional center-point click simulation
- DPI-aware cross-monitor scaling (`GetDpiForMonitor`)
- Atomic move/resize/topmost (`SetWindowPos`)
- Layered opacity (`SetWindowLongPtr` + `SetLayeredWindowAttributes`)

This gives reliable behavior across mixed DPI and multi-display environments.

---

## Architecture

Project pattern: **MVVM**

### High-level structure

- `Models/` – app process entries, monitor info, preferences schema, zones, move request
- `ViewModels/` – UI state + commands + autosave behavior
- `Services/` – process discovery, monitor enumeration, preferences persistence, move engine
- `Helpers/` – native interop (`NativeMethods`)
- `Commands/` – relay command implementation
- `Converters/` – UI converter for zone toggle state

### Startup behavior

- `App.xaml.cs`
  - Detects command-line mode vs GUI mode
  - Routes execution accordingly

---

## Requirements

- Windows (desktop)
- .NET 10 runtime/SDK
- Multi-monitor setup recommended for best experience

---

## Build & Run

```powershell
dotnet build
dotnet run --project ScreenHopper/ScreenHopper.csproj
```

CLI example:

```powershell
dotnet run --project ScreenHopper/ScreenHopper.csproj -- --move "notepad" --monitor 1 --zone "TopRight"
```

---

## Design Goals

- Fast interaction
- Minimal friction
- Reliable native behavior
- Scriptable automation
- Clean, maintainable MVVM codebase

---

## Roadmap Ideas

- Global hotkeys for quick snap profiles
- Saved monitor+zone presets
- Tray mode and background daemon option
- Import/export profile bundles
- More advanced window filters and matching rules

---

## Project Name

**ScreenHopper**: because your windows shouldn’t crawl—they should hop.
