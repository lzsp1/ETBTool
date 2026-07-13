using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ETBTool.Models;

namespace ETBTool.Utils
{
    public static class LanguageManager
    {
        public static bool IsChinese { get; private set; } = true;
        public static event Action? LanguageChanged;

        public static void SetLanguage(bool chinese)
        {
            if (IsChinese == chinese) return;
            IsChinese = chinese;
            SaveLanguage();
            LanguageChanged?.Invoke();
        }

        public static void LoadLanguage()
        {
            try
            {
                if (!File.Exists(GamePaths.SettingsFile)) return;
                var r = JsonDocument.Parse(File.ReadAllText(GamePaths.SettingsFile)).RootElement;
                if (r.TryGetProperty("Language", out var lang))
                    IsChinese = (lang.GetString() ?? "zh") == "zh";
            }
            catch { }
        }

        private static void SaveLanguage()
        {
            try
            {
                var dict = new Dictionary<string, string>();
                if (File.Exists(GamePaths.SettingsFile))
                    foreach (var p in JsonDocument.Parse(File.ReadAllText(GamePaths.SettingsFile))
                        .RootElement.EnumerateObject())
                        if (p.Value.ValueKind == JsonValueKind.String)
                            dict[p.Name] = p.Value.GetString() ?? "";
                dict["Language"] = IsChinese ? "zh" : "en";
                File.WriteAllText(GamePaths.SettingsFile,
                    JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private static readonly Dictionary<string, string> LogTranslations = new()
        {
            ["应用已启动"] = "App started",
            ["已启动"] = "Started",
            ["自动更新检查: "] = "Update check: ",
            ["更新检查: "] = "Update check: ",
            ["强调色: "] = "Accent: ",
            ["已加载"] = "Loaded",
            ["条翻译"] = "translations",
            ["翻译加载失败: "] = "Translation load failed: ",
            ["读取失败: "] = "Read failed: ",
            ["当前已是最新版本"] = "Up to date",
            ["发现新版本"] = "New version",
            ["检查更新超时了"] = "Update check timed out",
            ["无法识别版本信息"] = "Cannot read version",
        };

        public static string TranslateLog(string entry)
        {
            if (IsChinese) return entry;
            var result = entry;
            foreach (var kv in LogTranslations)
                result = result.Replace(kv.Key, kv.Value);
            return result;
        }

        // 导航
        public static string NavSave => IsChinese ? "存档管理" : "Saves";
        public static string NavCrash => IsChinese ? "崩溃日志" : "Crashes";
        public static string NavUE4 => IsChinese ? "UE4管理" : "UE4 Mgr";
        public static string NavMod => IsChinese ? "Mod管理" : "Mod Mgr";
        public static string NavDownload => IsChinese ? "下载Mod" : "Get Mods";
        public static string NavPaths => IsChinese ? "查看路径" : "Paths";
        public static string NavLog => IsChinese ? "日志" : "Logs";
        public static string NavSettings => IsChinese ? "设置" : "Settings";
        public static string NavAbout => IsChinese ? "关于" : "About";
        public static string NavUpdate => IsChinese ? "检查更新" : "Updates";

        // 通用按钮
        public static string BtnOpen => IsChinese ? "打开" : "Open";
        public static string BtnCopy => IsChinese ? "复制" : "Copy";
        public static string BtnRefresh => IsChinese ? "刷新" : "Refresh";
        public static string BtnDelete => IsChinese ? "删除选中" : "Delete";
        public static string BtnConfirm => IsChinese ? "确定" : "OK";
        public static string BtnCancel => IsChinese ? "取消" : "Cancel";
        public static string BtnYes => IsChinese ? "是" : "Yes";
        public static string BtnNo => IsChinese ? "否" : "No";
        public static string BtnReset => IsChinese ? "重置" : "Reset";
        public static string BtnApply => IsChinese ? "应用" : "Apply";
        public static string BtnBrowse => IsChinese ? "浏览" : "Browse";

        // 表头
        public static string ColName => IsChinese ? "名称" : "Name";
        public static string ColType => IsChinese ? "类型" : "Type";
        public static string ColSize => IsChinese ? "大小" : "Size";
        public static string ColModified => IsChinese ? "修改时间" : "Modified";
        public static string ColStatus => IsChinese ? "状态" : "Status";
        public static string ColFile => IsChinese ? "文件" : "File";
        public static string ColFolder => IsChinese ? "文件夹" : "Folder";

        // 通用消息
        public static string MsgCopied => IsChinese ? "已复制" : "Copied";
        public static string MsgCopyFail => IsChinese ? "复制失败" : "Failed";
        public static string MsgRefreshed => IsChinese ? "已刷新" : "Refreshed";
        public static string MsgStarted => IsChinese ? "已启动" : "Started";
        public static string MsgOpened => IsChinese ? "已打开" : "Opened";
        public static string MsgUpDir => IsChinese ? "..  (上级目录)" : "..  (Parent)";
        public static string MsgNoUpdate => IsChinese ? "当前已是最新版本" : "Up to date";

        // 存档管理
        public static string SaveTitle => IsChinese ? "存档管理" : "Save Mgr";
        public static string SaveSub => IsChinese ? "管理你的游戏存档文件（支持 Ctrl+点击多选）" : "Manage saves (Ctrl+click multi-select)";
        public static string SavePath => IsChinese ? "存档路径" : "Save Path";
        public static string SaveList => IsChinese ? "存档文件列表" : "Save Files";
        public static string ColDiff => IsChinese ? "难度" : "Diff.";
        public static string ColSaveName => IsChinese ? "存档名称" : "Name";
        public static string BtnEdit => IsChinese ? "编辑选中" : "Edit";
        public static string BtnClearAll => IsChinese ? "清理全部" : "Clear All";
        public static string DiffEasy => IsChinese ? "简单" : "Easy";
        public static string DiffNormal => IsChinese ? "普通" : "Norm";
        public static string DiffHard => IsChinese ? "困难" : "Hard";
        public static string DiffNight => IsChinese ? "噩梦" : "Night";
        public static string DiffPractice => IsChinese ? "练习" : "Prac";
        public static string DiffTutorial => IsChinese ? "教程" : "Tut";
        public static string MsgSaveLoaded(int n) => IsChinese ? $"已加载 {n} 个存档" : $"Loaded {n} saves";
        public static string MsgSaveDeleted(int n) => IsChinese ? $"已删除 {n} 个" : $"Deleted {n}";
        public static string MsgConfirmDelete(int n) => IsChinese ? $"确定删除 {n} 个存档？" : $"Delete {n} saves?";
        public static string MsgConfirmClear => IsChinese ? "确定清理全部存档？不可撤销！" : "Clear all saves? Cannot undo!";
        public static string MsgCleared => IsChinese ? "清理完成" : "Cleared";

        // 崩溃日志
        public static string CrashTitle => IsChinese ? "崩溃日志" : "Crash Logs";
        public static string CrashSub => IsChinese ? "查看和管理崩溃日志（支持 Ctrl+点击多选）" : "View crash logs (Ctrl+click)";
        public static string CrashPath => IsChinese ? "日志路径" : "Log Path";
        public static string CrashList => IsChinese ? "日志文件列表" : "Log Files";
        public static string BtnOpenSel => IsChinese ? "打开选中" : "Open";
        public static string BtnClearLogs => IsChinese ? "清空全部" : "Clear All";
        public static string MsgLogLoaded(int n) => IsChinese ? $"已加载 {n} 个日志" : $"Loaded {n} logs";
        public static string MsgConfirmDelLog(int n) => IsChinese ? $"确定删除 {n} 个日志？" : $"Delete {n} logs?";
        public static string MsgConfirmClearLogs => IsChinese ? "确定清空全部崩溃日志？" : "Clear all crash logs?";
        public static string MsgCrashLogCleared => IsChinese ? "已清空" : "Cleared";

        // UE4 管理
        public static string UE4Title => IsChinese ? "UE4 管理" : "UE4 Mgr";
        public static string UE4Sub => IsChinese ? "管理游戏安装目录文件（支持 Ctrl+点击多选、双击进入文件夹）" : "Manage game dir (Ctrl+click, dbl-click)";
        public static string UE4GamePath => IsChinese ? "游戏安装路径" : "Game Path";
        public static string UE4Contents => IsChinese ? "目录内容" : "Contents";
        public static string BtnUpDir => IsChinese ? "上级目录" : "Up Dir";
        public static string BtnImport => IsChinese ? "导入文件" : "Import";
        public static string BtnWipeAll => IsChinese ? "一键清空" : "Wipe All";
        public static string MsgConfirmDelItems(int n) => IsChinese ? $"确定删除 {n} 个项目？" : $"Delete {n} items?";
        public static string MsgWipeConfirm => IsChinese
            ? "一键全部清空游戏文件？\n\n清空后将自动打开 Steam 库页面，\n您可以在此重新下载/验证游戏文件。\n\n不可撤销！"
            : "Wipe all game files?\n\nSteam library will open after.\nCannot undo!";
        public static string MsgWiped => IsChinese ? "已清空，正在打开 Steam 库页面" : "Wiped, opening Steam";

        // Mod 管理
        public static string ModTitle => IsChinese ? "Mod 管理" : "Mod Mgr";
        public static string ModSub => IsChinese ? "管理已安装的 Mod（自动翻译、Ctrl+点击多选、双击进入文件夹）" : "Manage mods (Ctrl+click, dbl-click)";
        public static string ModPath => IsChinese ? "Mod 安装路径" : "Mod Path";
        public static string ModList => IsChinese ? "Mod 文件列表" : "Mod Files";
        public static string ModLoading => IsChinese ? "（翻译加载中...）" : "(Loading...)";
        public static string BtnImportMod => IsChinese ? "导入Mod" : "Import";
        public static string BtnPack => IsChinese ? "打包选中" : "Pack";
        public static string ModActions => IsChinese ? "Mod 操作" : "Mod Actions";
        // ★ "Toggle" → "Enable/Disable"
        public static string BtnToggle => IsChinese ? "禁用/启用" : "Enable/Disable";
        public static string BtnEnable => IsChinese ? "启用选中" : "Enable";
        public static string BtnDisable => IsChinese ? "禁用选中" : "Disable";
        public static string ModNickDesc => IsChinese ? "如果你的模组没被收录中文名，可以提交昵称帮助完善：" : "Submit mod nicknames:";
        public static string BtnSubmitNick => IsChinese ? "提交 Mod 昵称" : "Submit Nick";
        public static string MsgPackOK(int n) => IsChinese ? $"已打包 {n} 个文件" : $"Packed {n} files";
        public static string MsgPackFail => IsChinese ? "打包失败" : "Pack failed";
        public static string MsgSelMod => IsChinese ? "请先选择 Mod 文件（支持 Ctrl+点击多选）" : "Select mods first (Ctrl+click)";
        public static string MsgToggle(int e, int d) => IsChinese ? $"启用:{e} 禁用:{d}" : $"On:{e} Off:{d}";
        public static string MsgImportMod(int n) => IsChinese ? $"已导入 {n} 个" : $"Imported {n}";
        public static string MsgDelMod(int n) => IsChinese ? $"已删除 {n} 个" : $"Deleted {n}";
        public static string MsgConfirmDelMod(int n) => IsChinese ? $"确定删除 {n} 个文件？" : $"Delete {n} files?";
        public static string PackTxtName => IsChinese ? "由逃离后室游戏工具打包生成" : "Packed by ETBTool";
        public static string PackFooter => IsChinese
            ? "逃离后室游戏工具遵循MIT开源协议，二创、下载工具请访问https://github.com/lzsp1/ETBTool"
            : "ETBTool is MIT licensed. Visit https://github.com/lzsp1/ETBTool";

        // 下载 Mod
        public static string DlTitle => IsChinese ? "下载 Mod" : "Get Mods";
        public static string DlSub => IsChinese ? "前往 Mod 下载站" : "Go to mod download site";
        public static string DlGo => IsChinese ? "前往下载" : "Go";
        public static string DlLangDetect => IsChinese ? ""
            : "English mode detected. Are you from China?\n\nYes: CN-friendly site\nNo: Nexus Mods";
        public static string DlLangTitle => IsChinese ? "语言" : "Language";

        // 路径
        public static string PathsTitle => IsChinese ? "查看路径" : "Paths";
        public static string PathsSub => IsChinese ? "一键复制游戏相关路径" : "Copy game paths";
        public static string PathsSave => IsChinese ? "存档路径" : "Save Path";
        public static string PathsCrash => IsChinese ? "崩溃日志路径" : "Crash Log";
        public static string PathsGame => IsChinese ? "游戏安装路径" : "Game Path";
        public static string PathsMod => IsChinese ? "Mod 安装路径" : "Mod Path";

        // 日志
        public static string LogTitle => IsChinese ? "日志" : "Logs";
        public static string LogSub => IsChinese ? "查看操作日志记录" : "View operation logs";
        public static string BtnClearLog => IsChinese ? "清空日志" : "Clear";
        public static string MsgConfirmClearLog => IsChinese ? "确定清空日志？" : "Clear logs?";
        public static string MsgLogCleared => IsChinese ? "已清空" : "Cleared";

        // 设置
        public static string SetTitle => IsChinese ? "设置" : "Settings";
        public static string SetSub => IsChinese ? "主题、颜色、路径配置" : "Theme, color, path";
        public static string SetStyle => IsChinese ? "  界面风格" : "  UI Style";
        public static string SetMode => IsChinese ? "  颜色模式" : "  Color Mode";
        public static string SetAccent => IsChinese ? "  自定义强调色" : "  Accent Color";
        public static string SetAccentDesc => IsChinese ? "点击色块更换主题强调色" : "Click swatch to change accent";
        public static string SetHexLabel => IsChinese ? "自定义 HEX:" : "Custom HEX:";
        public static string SetBg => IsChinese ? "  背景图片" : "  Background";
        public static string SetBgDesc => IsChinese ? "上传图片作为背景（菜单栏和内容区通铺）" : "Upload image as background";
        public static string BtnChooseImg => IsChinese ? "选择图片" : "Choose";
        public static string BtnClearBg => IsChinese ? "清除背景" : "Clear";
        public static string SetBgNone => IsChinese ? "未设置" : "Not set";
        public static string SetPaths => IsChinese ? "  自定义路径（留空自动检测）" : "  Custom Paths (auto if blank)";
        public static string SetSaveLabel => IsChinese ? "存档路径" : "Save Path";
        public static string SetCrashLabel => IsChinese ? "崩溃日志路径" : "Crash Path";
        public static string SetGameLabel => IsChinese ? "游戏安装路径" : "Game Path";
        public static string BtnSavePaths => IsChinese ? "  保存路径设置" : "  Save Paths";
        public static string SetLanguageLabel => IsChinese ? "  语言" : "  Language";
        public static string SetLangDesc => IsChinese ? "切换界面语言（立即生效）" : "Switch language (instant)";
        public static string LangChinese => "中文";
        public static string LangEnglish => "English";
        public static string MsgAccentChanged => IsChinese ? "  强调色已更改" : "Accent changed";
        public static string MsgAccentReset => IsChinese ? "  已恢复默认强调色" : "Accent reset";
        public static string MsgBgSet => IsChinese ? "  背景图片已设置" : "Background set";
        public static string MsgBgClear => IsChinese ? "  背景已清除" : "Background cleared";
        public static string MsgPathsSaved => IsChinese ? "  路径设置已保存" : "Paths saved";
        public static string MsgStyleChanged(string n) => IsChinese ? $"  已切换为 {n}" : $"Switched to {n}";
        public static string MsgModeChanged(string n) => IsChinese ? $"  颜色模式: {n}" : $"Mode: {n}";
        public static string MsgHexError => IsChinese ? "  颜色格式错误，示例: #FF6B6B" : "Invalid color. Ex: #FF6B6B";

        // 关于
        public static string AboutTitle => IsChinese ? "关于" : "About";
        public static string AboutDesc => IsChinese ? "逃离后室（Escape the Backrooms）游戏辅助工具" : "Escape the Backrooms game utility";
        public static string AboutFeatures => IsChinese ? "功能" : "Features";
        public static string AboutFeatureList => IsChinese
            ? "存档管理、崩溃日志查看、UE4 资源管理、Mod 管理与翻译、游戏路径自动检测、主题切换与自定义、一键更新检查"
            : "Save mgr, crash logs, UE4 res mgr, mod mgr, path detect, themes, auto-update";
        public static string AboutLicense => IsChinese ? "开源协议" : "License";
        public static string AboutLicenseText => IsChinese
            ? "本工具基于 MIT 开源协议发布，欢迎二创与分发。"
            : "MIT License. Forks welcome.";
        public static string BtnConfig => IsChinese ? "打开配置目录" : "Config Dir";
        public static string BtnWebsite => IsChinese ? "官方网站" : "Website";
        public static string BtnSource => IsChinese ? "开源仓库" : "Source";

        // 检查更新
        public static string UpdateTitle => IsChinese ? "检查更新" : "Check Updates";
        public static string UpdateCurVer => IsChinese ? "当前版本" : "Current Version";
        public static string UpdateChecking => IsChinese ? "正在检查更新..." : "Checking...";
        public static string UpdateFound(string v) => IsChinese ? $"发现新版本 v{v}" : $"New version v{v}";
        public static string UpdateLatest => IsChinese ? "最新版本" : "Latest";
        public static string UpdateCurrent => IsChinese ? "当前版本" : "Current";
        public static string UpdateHint => IsChinese ? "请前往下载页面获取最新版本。" : "Go to download page.";
        public static string BtnDL => IsChinese ? "前往下载" : "Download";
        public static string BtnViewLog => IsChinese ? "查看更新记录" : "Changelog";
        public static string BtnCheckUpdate => IsChinese ? "检查更新" : "Check Updates";
        public static string UpdateLogSection => IsChinese ? "更新日志" : "Changelog";

        // 提示
        public static string TipOpenPath => IsChinese
            ? "路径跳转 Bug 正在全力修复中，请您手动粘贴路径。\n\n点击\"确定\"将自动复制路径并打开资源管理器，\n点击\"取消\"不做任何操作。"
            : "Path redirect bug. Click OK to copy path and open Explorer,\nCancel to do nothing.";

        // 主题
        public static string[] ThemeNames => IsChinese
            ? new[] { "全玻璃风格", "极简留白", "玻璃拟态", "新拟态", "3D立体风", "霓虹彩色" }
            : new[] { "Glass", "Minimal", "Glass Morph", "Neumorph", "3D", "Neon" };

        public static string[] ModeNames => IsChinese
            ? new[] { "浅色", "深色", "跟随系统" }
            : new[] { "Light", "Dark", "System" };
    }
}