# ETB Tool

**逃离后室（Escape the Backrooms）游戏辅助工具**

![Version](https://img.shields.io/badge/version-0.0.02-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)

---

## 📖 目录

- [功能一览](#-功能一览)
- [支持的主题风格](#-支持的主题风格)
- [项目结构](#-项目结构)
- [环境要求](#-环境要求)
- [构建与运行](#-构建与运行)
- [版本更新检测](#-版本更新检测)
- [Mod 翻译数据](#-mod-翻译数据)
- [游戏路径自动检测](#-游戏路径自动检测)
- [存档文件命名规则](#-存档文件命名规则)
- [Mod 禁用机制](#-mod-禁用机制)
- [Mod 打包功能](#-mod-打包功能)
- [配置文件位置](#-配置文件位置)
- [窗口操作](#-窗口操作)
- [协议](#-协议)
- [相关链接](#-相关链接)

---

## ✨ 功能一览

| 功能 | 说明 |
|------|------|
| **存档管理** | 查看、编辑、删除游戏存档，支持 `Ctrl+点击` 多选，自动识别难度标签 |
| **崩溃日志** | 查看和管理崩溃日志文件，支持多选删除 |
| **UE4 管理** | 浏览游戏安装目录（Win64），支持导入文件、删除、一键清空后跳转 Steam |
| **Mod 管理** | 安装、禁用（`.disabled` 后缀）、翻译、打包（ZIP + 信息文档），支持 `Ctrl+点击` 多选 |
| **下载 Mod** | 跳转至 Mod 下载站 |
| **查看路径** | 一键查看并复制存档、崩溃日志、游戏安装、Mod 安装路径 |
| **日志** | 实时查看操作日志 |
| **设置** | 6 种主题风格、浅色/深色/跟随系统、自定义强调色、背景图片、自定义路径 |
| **关于** | 项目信息、打开配置目录、官方网站、开源仓库 |
| **检查更新** | 从 GitHub 自动检测新版本，支持国内镜像加速下载 |

---

## 🎨 支持的主题风格

| 主题 | 说明 |
|------|------|
| 全玻璃风格 | 毛玻璃效果 |
| 极简留白 | 简洁明亮 |
| 玻璃拟态 | Glassmorphism |
| 新拟态 | Neumorphism |
| 3D 立体风 | 带有立体感的卡片 |
| 霓虹彩色 | 霓虹灯光效果 |

颜色模式支持 **浅色**、**深色**、**跟随系统** 三种切换。

---

## 📁 项目结构

```
ETBTool/
├── Models/
│   └── GamePaths.cs               # 路径管理、版本号、Steam AppID
├── Themes/
│   ├── BaseTheme.xaml             # 基础样式（按钮、列表、卡片等）
│   ├── ThemeManager.cs            # 主题管理器
│   ├── GlassTheme.xaml
│   ├── MinimalTheme.xaml
│   ├── GlassMorphismTheme.xaml
│   ├── NeumorphismTheme.xaml
│   ├── ThreeDTheme.xaml
│   └── NeonTheme.xaml
├── Utils/
│   ├── Logger.cs                  # 日志工具
│   ├── Toast.cs                   # Toast 通知组件
│   ├── ScrollBubble.cs            # 滚动穿透行为
│   └── UpdateChecker.cs           # 更新检测与下载
├── Windows/
│   └── ThemedDialog.cs            # 自定义弹窗
├── Views/
│   ├── SaveManagerPage.xaml/.cs   # 存档管理
│   ├── CrashLogPage.xaml/.cs      # 崩溃日志
│   ├── UE4ManagerPage.xaml/.cs    # UE4 管理
│   ├── ModManagerPage.xaml/.cs    # Mod 管理
│   ├── DownloadModPage.xaml/.cs   # 下载 Mod
│   ├── PathsPage.xaml/.cs         # 查看路径
│   ├── LogPage.xaml/.cs           # 日志
│   ├── AboutPage.xaml/.cs         # 关于
│   ├── SettingsPage.xaml/.cs      # 设置
│   └── UpdatePage.xaml/.cs        # 检查更新
├── MainWindow.xaml/.cs            # 主窗口
├── App.xaml/.cs                   # 应用入口
└── ETBTool.csproj                 # 项目文件
```

---

## 🖥️ 环境要求

- **操作系统**：Windows 10 / 11（64 位）
- **运行时**：.NET 8.0 Desktop Runtime
- **目标框架**：`net8.0-windows`
- **发布方式**：单文件发布（win-x64）

---

## 🔧 构建与运行

```bash
# 克隆仓库
git clone https://github.com/lzsp1/ETBTool.git
cd ETBTool

# 还原依赖
dotnet restore

# 编译
dotnet build -c Release

# 发布单文件
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

---

## 🔄 版本更新检测

程序启动时会自动从以下地址获取最新版本信息：

```
https://raw.githubusercontent.com/lzsp1/ETBTool/refs/heads/main/version.json
```

### `version.json` 格式

```json
{
  "latest_version": "0.0.02",
  "download_url": "https://github.com/lzsp1/ETBTool/releases/download/V0.0.02/ETBTool.V0.0.02.zip",
  "mirror_url": "https://ghfast.top/https://github.com/lzsp1/ETBTool/releases/download/V0.0.02/ETBTool.V0.0.02.zip"
}
```

| 字段 | 说明 | 必填 |
|------|------|------|
| `latest_version` | 最新版本号 | 是 |
| `download_url` | 原始 GitHub 下载链接 | 否 |
| `mirror_url` | 国内镜像加速链接 | 否 |

> 程序优先使用 `mirror_url`，其次 `download_url`，均失败时自动跳转至 GitHub Releases 页面。

**国内镜像前缀可选**：`https://ghfast.top/`、`https://ghproxy.com/`、`https://gh-proxy.com/`

---

## 🌐 Mod 翻译数据

Mod 中文名称数据从以下地址获取：

```
https://raw.githubusercontent.com/lzsp1/ETBTool/refs/heads/main/mod_names.json
```

### 格式（对象或数组均可）

**对象格式：**

```json
{
  "SomeMod.pak": "某模组中文名",
  "AnotherMod.pak": "另一个模组"
}
```

**数组格式：**

```json
[
  { "filename": "SomeMod.pak", "name": "某模组中文名" },
  { "filename": "AnotherMod.pak", "name": "另一个模组" }
]
```

---

## 🗂️ 游戏路径自动检测

| 路径 | 默认值 |
|------|--------|
| 存档 | `%LOCALAPPDATA%\EscapeTheBackrooms\Saved\SaveGames` |
| 崩溃日志 | `%LOCALAPPDATA%\EscapeTheBackrooms\Saved\Crashes` |
| UE4 目录 | `<GameInstallPath>\EscapeTheBackrooms\Binaries\Win64` |
| Mod 目录 | `<GameInstallPath>\EscapeTheBackrooms\Content\Paks` |

游戏安装路径会自动扫描 Steam 库文件夹，支持多盘符、自定义 Steam 库路径。用户也可在设置中手动指定路径。

---

## 💾 存档文件命名规则

仅识别以下格式的存档文件：

```
MULTIPLAYER_<存档名>_<难度>.sav
```

支持的难度标签：

| 标签 | 中文 | 颜色 |
|------|------|------|
| `Easy` | 简单 | 绿色 |
| `Normal` | 普通 | 蓝色 |
| `Hard` | 困难 | 橙色 |
| `Nightmare` | 噩梦 | 红色 |

> 不符合命名规则的文件将被自动隐藏。

---

## 🚫 Mod 禁用机制

- **禁用**：在文件名末尾添加 `.disabled` 后缀  
  `mod.pak  →  mod.pak.disabled`

- **启用**：去掉 `.disabled` 后缀  
  `mod.pak.disabled  →  mod.pak`

---

## 📦 Mod 打包功能

选中一个或多个 Mod 文件（支持 `Ctrl+点击` 多选），点击 **"打包选中"** 后会生成一个 `.zip` 压缩包，包含：

- 所有选中的 Mod 文件
- `由逃离后室游戏工具打包生成.txt`（包含打包信息和协议声明）

---

## ⚙️ 配置文件位置

```
%APPDATA%\ETBTool\
├── settings.json    # 主题、路径等配置
└── operation.log    # 操作日志
```

---

## 🪟 窗口操作

- 支持自定义无边框窗口
- 标题栏拖拽移动
- 双击标题栏最大化/还原
- 最小化、最大化、关闭按钮带有悬停动画效果
- 关闭按钮悬停时红色高亮
- 侧边栏收起/展开动画

---

## 📄 协议

本项目基于 **MIT License** 开源。

欢迎二创、分发。如需提交 Mod 中文翻译数据，请通过 [Issues](https://github.com/lzsp1/ETBTool/issues) 提交。

---

## 🔗 相关链接

| 用途 | 链接 |
|------|------|
| 开源仓库 | [https://github.com/lzsp1/ETBTool](https://github.com/lzsp1/ETBTool) |
| 官方网站 | [https://taolihoushiwanyouqun.wordpress.com/](https://taolihoushiwanyouqun.wordpress.com/) |
| Mod 下载站 | [https://etbtoolmod.xn--online-o20ki81q.top/](https://etbtoolmod.xn--online-o20ki81q.top/) |
| Releases 下载 | [https://github.com/lzsp1/ETBTool/releases](https://github.com/lzsp1/ETBTool/releases) |
| 提交 Mod 昵称 | [https://github.com/lzsp1/ETBTool/issues](https://github.com/lzsp1/ETBTool/issues) |
```
