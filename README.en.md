```
# ETB Tool

**Escape the Backrooms Game Assistant Tool**
- [中文版](https://github.com/lzsp1/ETBTool/blob/main/README.md)

---

## 📖 Table of Contents

- [Feature Overview](#-feature-overview)
- [Supported Theme Styles](#-supported-theme-styles)
- [Project Structure](#-project-structure)
- [Environment Requirements](#-environment-requirements)
- [Build & Run](#-build--run)
- [Version Update Checker](#-version-update-checker)
- [Mod Translation Data](#-mod-translation-data)
- [Automatic Game Path Detection](#-automatic-game-path-detection)
- [Save File Naming Rule](#-save-file-naming-rule)
- [Mod Disable Mechanism](#-mod-disable-mechanism)
- [Mod Packing Function](#-mod-packing-function)
- [Config File Location](#-config-file-location)
- [Window Controls](#-window-controls)
- [License](#-license)
- [Related Links](#-related-links)

---

## ✨ Feature Overview

| Feature | Description |
|------|------|
| **Save Management** | View, edit and delete game saves. Support multi-selection with `Ctrl+Click`. Automatically recognize difficulty tags |
| **Crash Logs** | View and manage crash log files, support bulk deletion |
| **UE4 Manager** | Browse the game Win64 installation folder. Support file import, deletion, one-click cleanup and jump to Steam |
| **Mod Manager** | Install / disable (`.disabled` suffix) / translate / pack (ZIP + info doc) mods. Support multi-selection with `Ctrl+Click` |
| **Mod Downloader** | Built-in mod library: search, category filter, pagination, detail pages and preview images. Two download options: direct download from this site or external link (open in browser). Files are cached to temp folder first, auto-install / save as / keep files after download completes |
| **Path Viewer** | One-click view and copy paths for saves, crash logs, game directory and mod folder |
| **Logger** | Real-time operation log viewer |
| **Settings** | 6 theme styles, Light / Dark / System-follow modes, custom accent color, custom background image, custom file paths |
| **About** | Project info, open config directory, official website, source repository |
| **Update Check** | Auto / manual new version detection, forced update supported. When download is blocked (HTTP 404), prompt to use mirror link and support manual ZIP installation via "Select File" |

---

## 🎨 Supported Theme Styles

| Theme | Description |
|------|------|
| Full Glass Style | Frosted glass effect |
| Minimal Blank | Clean and bright layout |
| Glassmorphism | Glassmorphism design |
| Neumorphism | Neumorphism design |
| 3D Card Style | Cards with 3D stereoscopic effects |
| Neon Color | Neon glowing visual effect |

Color modes: **Light**, **Dark**, **Follow System**.

---

## 📁 Project Structure
```

.
├── App.xaml / App.xaml.cs           # Application Entry
├── MainWindow.xaml / .cs            # Main Window
├── ETBTool.csproj                   # Project File
├── ETBTool.sln                      # Solution File
├── Models/
│   ├── ApiModels.cs                 # API Data Models
│   └── GamePaths.cs                 # Path Manager, Version & Steam AppID
├── Services/
│   ├── ModApiClient.cs              # Mod Library API Client
│   ├── ModFileOperations.cs         # Mod File Operations
│   ├── ModTranslationService.cs     # Mod Translation Service
│   ├── ConfigPersistence.cs         # Configuration Storage
│   └── BackgroundMusicService.cs    # Background Audio Service
├── Utils/
│   ├── Logger.cs                    # Logging Utility
│   ├── Toast.cs                     # Toast Notification Component
│   ├── ScrollBubble.cs              # Scroll-through Behaviour
│   ├── ExternalLauncher.cs          # Open Explorer / Browser
│   ├── RuntimeEnvironment.cs        # Runtime Environment Detector
│   └── UpdateChecker.cs             # Update Check, Download & Install
├── ViewModels/
│   └── ModBrowserViewModel.cs       # Mod Library ViewModel
├── Views/
│   ├── SaveManagerPage.xaml/.cs     # Save Manager
│   ├── CrashLogPage.xaml/.cs        # Crash Log Manager
│   ├── UE4ManagerPage.xaml/.cs      # UE4 Manager
│   ├── ModManagerPage.xaml/.cs      # Mod Manager
│   ├── DownloadModPage.xaml/.cs     # Mod Library Download Page
│   ├── PathsPage.xaml/.cs           # Path Viewer
│   ├── LogPage.xaml/.cs             # Log Viewer
│   ├── AboutPage.xaml/.cs           # About Page
│   ├── SettingsPage.xaml/.cs        # Settings Page
│   └── UpdatePage.xaml/.cs          # Update Check Page
├── Windows/                         # Custom Popups & Editor Windows
├── Themes/                          # 6 Theme Resource Dictionaries
├── version.json                     # Release Version Metadata
└── mod_names.json                   # Mod Name Translation Data

```

---

## 🖥️ Environment Requirements

- **OS**: Windows 10 / 11 (64-bit)
- **Runtime**: .NET 8.0 Desktop Runtime
- **Target Framework**: `net8.0-windows`
- **Publish Mode**: Single-file publish (win-x64)

---

## 🔧 Build & Run

```bash
# Clone repository
git clone [https://github.com/lzsp1/ETBTool.git](https://github.com/lzsp1/ETBTool.git)
cd ETBTool

# Restore dependencies
dotnet restore

# Build project
dotnet build -c Release

# Publish single executable
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

---

## 🔄 Version Update Checker

On startup, the tool fetches the latest release info from:

```
[https://raw.githubusercontent.com/lzsp1/ETBTool/refs/heads/main/version.json](https://raw.githubusercontent.com/lzsp1/ETBTool/refs/heads/main/version.json)
```

### `version.json` Schema

```
{
  "latest_version": "0.0.02",
  "download_url": "[https://github.com/lzsp1/ETBTool/releases/download/V0.0.02/ETBTool.V0.0.02.zip](https://github.com/lzsp1/ETBTool/releases/download/V0.0.02/ETBTool.V0.0.02.zip)",
  "mirror_url": "shturl.cc/HvlcbmJ4oxxBOXYGL8GvBLnoYhHeTh555iTWXJ4Eit82PzYWMkcDlcAOzcWxh21EQlF3Xi6RoRi1odCBoegFK0m"
}
```

| Field | Description | Required |
| --- | --- | --- |
| `latest_version` | Latest version tag | Yes |
| `download_url` | Original GitHub download link | No |
| `mirror_url` | Accelerated mirror link for China | No |

> 
> Priority: `mirror_url` first, then `download_url`. If both fail, open GitHub Releases webpage automatically.

**Available mirror prefixes**: `shturl.cc/OOxh2UCrK`, `[https://ghproxy.com/](https://ghproxy.com/)`, `[https://gh-proxy.com/](https://gh-proxy.com/)`

---

## 🌐 Mod Translation Data

Mod name translation data is fetched from:

```
[https://raw.githubusercontent.com/lzsp1/ETBTool/refs/heads/main/mod_names.json](https://raw.githubusercontent.com/lzsp1/ETBTool/refs/heads/main/mod_names.json)
```

### Supported Formats (Object or Array)

**Object Format:**

```
{
  "SomeMod.pak": "Some Mod",
  "AnotherMod.pak": "Another Mod"
}
```

**Array Format:**

```
[
  { "filename": "SomeMod.pak", "name": "Some Mod" },
  { "filename": "AnotherMod.pak", "name": "Another Mod" }
]
```

---

## 🗂️ Automatic Game Path Detection

| Path | Default Location |
| --- | --- |
| Save Folder | `%LOCALAPPDATA%\EscapeTheBackrooms\Saved\SaveGames` |
| Crash Log Folder | `%LOCALAPPDATA%\EscapeTheBackrooms\Saved\Crashes` |
| UE4 Directory | `<GameInstallPath>\EscapeTheBackrooms\Binaries\Win64` |
| Mod Directory | `<GameInstallPath>\EscapeTheBackrooms\Content\Paks` |

The program scans Steam library folders automatically, supporting multiple disks and custom Steam library locations. Manual path override is available in Settings.

---

## 💾 Save File Naming Rule

Only files matching this pattern will be recognized:

```
MULTIPLAYER_<SaveName>_<Difficulty>.sav
```

Supported difficulty tags:

| Tag | Chinese | Color |
| --- | --- | --- |
| `Easy` | Easy | Green |
| `Normal` | Normal | Blue |
| `Hard` | Hard | Orange |
| `Nightmare` | Nightmare | Red |

> 
> Files that do not match the naming rule will be hidden automatically.

---

## 🚫 Mod Disable Mechanism

- **Disable**: Append `.disabled` suffix to filename
`mod.pak  →  mod.pak.disabled`
- **Enable**: Remove the `.disabled` suffix
`mod.pak.disabled  →  mod.pak`

---

## 📦 Mod Packing Function

Select one or multiple mod files (multi-select with `Ctrl+Click`), click **Pack Selected**, it will generate a `.zip` archive containing:

- All selected mod files
- `Generated by Escape the Backrooms Game Tool.txt` (contains packing info and license statement)

---

## ⚙️ Config File Location

```
%APPDATA%\ETBTool\
├── settings.json    # Theme, path and other configurations
└── operation.log    # Operation logs
```

---

## 🪟 Window Controls

- Custom borderless window supported
- Drag title bar to move window
- Double-click title bar to maximize / restore
- Minimize, maximize and close buttons with hover animation
- Close button highlights red on hover
- Sidebar collapse / expand animation

---

## 📄 License

This project is open-source under the **MIT License**.

Modification and redistribution are welcome. If you want to submit mod Chinese translation data, please submit via [Feishu Form](https://acnq9mbub8ek.feishu.cn/share/base/form/shrcnU6cBUdmrLjyKxltPlcwweg).

---

## 🔗 Related Links

| Purpose | Link |
| --- | --- |
| Source Repository | [](https://github.com/lzsp1/ETBTool)[https://github.com/lzsp1/ETBTool](https://github.com/lzsp1/ETBTool) |
| Official Website | [](https://taolihoushiwanyouqun.wordpress.com/)[https://taolihoushiwanyouqun.wordpress.com/](https://taolihoushiwanyouqun.wordpress.com/) |
| Mod Download Site | [](https://backroomsmod.xn--online-o20ki81q.top/)[https://backroomsmod.xn--online-o20ki81q.top/](https://backroomsmod.xn--online-o20ki81q.top/) |
| Releases Download | [](https://github.com/lzsp1/ETBTool/releases)[https://github.com/lzsp1/ETBTool/releases](https://github.com/lzsp1/ETBTool/releases) |
| Submit Mod Names | [https://acnq9mbub8ek.feishu.cn/share/base/form/shrcnU6cBUdmrLjyKxltPlcwweg](https://acnq9mbub8ek.feishu.cn/share/base/form/shrcnU6cBUdmrLjyKxltPlcwweg) |
| ``` |  |
