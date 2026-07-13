# ETB Tool

**Escape the Backrooms Game Utility Tool**

---

## 📖 Table of Contents

- [Feature Overview](#-feature-overview)
- [Supported Theme Styles](#-supported-theme-styles)
- [Project Structure](#-project-structure)
- [System Requirements](#-system-requirements)
- [Build & Run Guide](#-build--run-guide)
- [Version Update Checker](#-version-update-checker)
- [Mod Translation Data](#-mod-translation-data)
- [Automatic Game Path Detection](#-automatic-game-path-detection)
- [Save File Naming Rules](#-save-file-naming-rules)
- [Mod Disable Mechanism](#-mod-disable-mechanism)
- [Mod Packaging Function](#-mod-packaging-function)
- [Config File Location](#-config-file-location)
- [Window Controls](#-window-controls)
- [License](#-license)
- [Related Links](#-related-links)

---

## ✨ Feature Overview

| Feature | Description |
| --- | --- |
| **Save Manager** | View, edit and delete game saves; multi-select via `Ctrl + Click`; auto recognize difficulty tags |
| **Crash Logs** | View and manage crash log files; bulk delete supported |
| **UE4 Manager** | Browse game Win64 installation directory; import/delete files; one-click clear and jump to Steam |
| **Mod Manager** | Install, disable (`.disabled` suffix), translate and package mods into ZIP with info document; multi-select via `Ctrl + Click` |
| **Mod Downloader** | Quick jump to mod download website |
| **Path Viewer** | One-click view and copy paths for saves, crash logs, game installation and mod folders |
| **Operation Logs** | Real-time view of all program operations |
| **Settings** | 6 built-in themes; Light/Dark/System-following color modes; custom accent color, background image and custom file paths |
| **About Page** | Project introduction, open config directory, official website and open-source repository links |
| **Update Checker** | Auto scan GitHub for new versions; download acceleration via domestic mirror links |

---

## 🎨 Supported Theme Styles

| Theme | Description |
| --- | --- |
| Full Glass Style | Frosted glass visual effect |
| Minimal White Space | Clean and bright interface |
| Glassmorphism | Classic glassmorphism design |
| Neumorphism | Soft neumorphic UI style |
| 3D Stereo Style | Cards with three-dimensional depth effects |
| Neon Color Style | Vibrant neon glow visual effects |

Three color modes are available: **Light**, **Dark**, and **Follow System**.

---

## 📁 Project Structure

```
ETBTool/
├── Models/
│   └── GamePaths.cs               # Path management, version number, Steam AppID
├── Themes/
│   ├── BaseTheme.xaml             # Base UI styles (buttons, lists, cards, etc.)
│   ├── ThemeManager.cs            # Theme management core
│   ├── GlassTheme.xaml
│   ├── MinimalTheme.xaml
│   ├── GlassMorphismTheme.xaml
│   ├── NeumorphismTheme.xaml
│   ├── ThreeDTheme.xaml
│   └── NeonTheme.xaml
├── Utils/
│   ├── Logger.cs                  # Logging utility
│   ├── Toast.cs                   # Popup toast notification component
│   ├── ScrollBubble.cs            # Scroll penetration behavior logic
│   └── UpdateChecker.cs           # Version detection and download module
├── Windows/
│   └── ThemedDialog.cs            # Custom styled popup dialogs
├── Views/
│   ├── SaveManagerPage.xaml/.cs   # Save management page
│   ├── CrashLogPage.xaml/.cs      # Crash log viewer page
│   ├── UE4ManagerPage.xaml/.cs    # UE4 directory management page
│   ├── ModManagerPage.xaml/.cs    # Mod management page
│   ├── DownloadModPage.xaml/.cs   # Mod download navigation page
│   ├── PathsPage.xaml/.cs         # Path viewing page
│   ├── LogPage.xaml/.cs           # Operation log page
│   ├── AboutPage.xaml/.cs         # About information page
│   ├── SettingsPage.xaml/.cs      # Global settings page
│   └── UpdatePage.xaml/.cs        # Update check page
├── MainWindow.xaml/.cs            # Main program window
├── App.xaml/.cs                   # Application entry point
└── ETBTool.csproj                 # Project configuration file
```

---

## 🖥️ System Requirements

- **Operating System**: Windows 10 / 11 (64-bit)
- **Runtime**: .NET 8.0 Desktop Runtime
- **Target Framework**: `net8.0-windows`
- **Deployment Method**: Single-file publish (win-x64)

---

## 🔧 Build & Run Guide

```
# Clone repository
git clone https://github.com/lzsp1/ETBTool.git
cd ETBTool

# Restore dependencies
dotnet restore

# Compile project
dotnet build -c Release

# Publish single-file executable
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

---

## 🔄 Version Update Checker

The program automatically fetches the latest version info from the below URL on startup:

```
https://raw.githubusercontent.com/lzsp1/ETBTool/refs/heads/main/version.json
```

### `version.json` Format

```
{
  "latest_version": "0.0.02",
  "download_url": "https://github.com/lzsp1/ETBTool/releases/download/V0.0.02/ETBTool.V0.0.02.zip",
  "mirror_url": "shturl.cc/s6V2gaPcwXIdwNQpYTiXrUvF4YSOKhgpG3rTdm6EJiITeLymMQzAP3W9uArbPD78VOcJ1nZ3p0zpMfnAWERwJss"
}
```

| Field | Description | Required |
| --- | --- | --- |
| `latest_version` | Latest available version number | Yes |
| `download_url` | Original GitHub release download link | No |
| `mirror_url` | Accelerated domestic mirror link | No |

> 
> The tool prioritizes `mirror_url`, then falls back to `download_url`. If both fail, it will automatically open the GitHub Releases page.

**Available domestic mirror prefixes**: `shturl.cc/r16imCvKD`, `https://ghproxy.com/`, `https://gh-proxy.com/`

---

## 🌐 Mod Translation Data

Chinese mod display names are pulled from the following remote file:

```
https://raw.githubusercontent.com/lzsp1/ETBTool/refs/heads/main/mod_names.json
```

### Supported Formats (Object or Array)

**Object Format:**

```
{
  "SomeMod.pak": "Chinese Mod Display Name",
  "AnotherMod.pak": "Another Mod Name"
}
```

**Array Format:**

```
[
  { "filename": "SomeMod.pak", "name": "Chinese Mod Display Name" },
  { "filename": "AnotherMod.pak", "name": "Another Mod Name" }
]
```

---

## 🗂️ Automatic Game Path Detection

| Path Type | Default Location |
| --- | --- |
| Save Files | `%LOCALAPPDATA%\EscapeTheBackrooms\Saved\SaveGames` |
| Crash Logs | `%LOCALAPPDATA%\EscapeTheBackrooms\Saved\Crashes` |
| UE4 Bin Directory | `<GameInstallPath>\EscapeTheBackrooms\Binaries\Win64` |
| Mod Pak Directory | `<GameInstallPath>\EscapeTheBackrooms\Content\Paks` |

The program auto-scans all Steam library folders across multiple disk drives to locate the game. Custom manual paths can also be set in Settings.

---

## 💾 Save File Naming Rules

Only saves matching the format below will be recognized:

```
MULTIPLAYER_<SaveName>_<Difficulty>.sav
```

Supported difficulty tags:

| Tag | Display Name | Color |
| --- | --- | --- |
| `Easy` | Easy | Green |
| `Normal` | Normal | Blue |
| `Hard` | Hard | Orange |
| `Nightmare` | Nightmare | Red |

> 
> Files that do not follow this naming convention will be hidden automatically.

---

## 🚫 Mod Disable Mechanism

- **Disable Mod**: Append `.disabled` suffix to filename
`mod.pak  →  mod.pak.disabled`
- **Enable Mod**: Remove the `.disabled` suffix
`mod.pak.disabled  →  mod.pak`

---

## 📦 Mod Packaging Function

Select one or multiple mod files (multi-select via `Ctrl + Click`), then click **Package Selected**. A `.zip` archive will be generated containing:

- All selected mod pak files
- `Generated by Escape the Backrooms Game Utility.txt` (contains packaging info and license statement)

---

## ⚙️ Config File Location

```
%APPDATA%\ETBTool\
├── settings.json    # Stores theme, custom paths and other preferences
└── operation.log    # Full program operation logs
```

---

## 🪟 Window Controls

- Custom borderless window design
- Drag title bar to move window
- Double-click title bar to maximize/restore window
- Minimize, maximize and close buttons with hover animation effects
- Close button highlights red on mouse hover
- Animated expand/collapse sidebar

---

## 📄 License

This project is open-sourced under the **MIT License**.

Modification and redistribution are permitted. To contribute Chinese mod name translations, submit via [Issues](https://github.com/lzsp1/ETBTool/issues).

---

## 🔗 Related Links

| Purpose | Link |
| --- | --- |
| Open Source Repository | [https://github.com/lzsp1/ETBTool](https://github.com/lzsp1/ETBTool) |
| Official Website | [https://taolihoushiwanyouqun.wordpress.com/](https://taolihoushiwanyouqun.wordpress.com/) |
| Mod Download Portal | [https://etbtoolmod.xn--online-o20ki81q.top/](https://etbtoolmod.xn--online-o20ki81q.top/) |
| Release Downloads | [https://github.com/lzsp1/ETBTool/releases](https://github.com/lzsp1/ETBTool/releases) |
| Submit Mod Name Translations | [https://github.com/lzsp1/ETBTool/issues](https://github.com/lzsp1/ETBTool/issues) |