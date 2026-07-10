using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ETBTool.Models
{
    public static class GamePaths
    {
        private static readonly string LocalAppData =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string AppData =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public static string AppConfigPath => Path.Combine(AppData, "ETBTool");
        public static string LogFilePath => Path.Combine(AppConfigPath, "operation.log");
        public static string SettingsFile => Path.Combine(AppConfigPath, "settings.json");

        public static string ModNamesUrl =>
            "https://raw.githubusercontent.com/lzsp1/ETBTool/refs/heads/main/mod_names.json";
        public static string UpdateCheckUrl =>
            "https://raw.githubusercontent.com/lzsp1/ETBTool/refs/heads/main/version.json";
        public static string UpdateDownloadUrl =>
            "https://github.com/lzsp1/ETBTool/releases";
        public static string ModDownloadUrl =>
            "https://etbtoolmod.xn--online-o20ki81q.top/";
        public static string OfficialWebsite =>
            "https://taolihoushiwanyouqun.wordpress.com/";
        public static string SourceCodeUrl =>
            "https://github.com/lzsp1/ETBTool";
        public static string NicknameSubmitUrl =>
            "https://github.com/lzsp1/ETBTool/issues";

        public const string DisplayVersion = "0.0.02";
        public const string SteamAppId = "1943950";

        public static string SteamGameUri => $"steam://rungameid/{SteamAppId}";

        private static string? _customSavePath, _customCrashPath, _customGamePath;

        public static string DefaultSavePath =>
            Path.Combine(LocalAppData, "EscapeTheBackrooms", "Saved", "SaveGames");
        public static string DefaultCrashPath =>
            Path.Combine(LocalAppData, "EscapeTheBackrooms", "Saved", "Crashes");

        public static string SavePath =>
            IsPathValid(_customSavePath) ? _customSavePath! : DefaultSavePath;
        public static string CrashLogPath =>
            IsPathValid(_customCrashPath) ? _customCrashPath! : DefaultCrashPath;

        public static string GameInstallPath
        {
            get
            {
                if (IsPathValid(_customGamePath)) return _customGamePath!;
                return AutoDetectGamePath();
            }
        }

        public static string UE4Path =>
            Path.Combine(GameInstallPath, "EscapeTheBackrooms", "Binaries", "Win64");

        public static string ModPath
        {
            get
            {
                var baseP = GameInstallPath;
                string[] candidates =
                {
                    Path.Combine(baseP, "EscapeTheBackrooms", "Content", "Paks"),
                    Path.Combine(baseP, "Content", "Paks"),
                    Path.Combine(baseP, "Paks"),
                };
                foreach (var c in candidates)
                    if (Directory.Exists(c)) return c;
                return candidates[0];
            }
        }

        public static string SteamPath
        {
            get
            {
                try
                {
                    using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                    var p = k?.GetValue("SteamPath") as string;
                    if (!string.IsNullOrEmpty(p)) return p;
                }
                catch { }
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
            }
        }

        private static bool IsPathValid(string? p) =>
            !string.IsNullOrWhiteSpace(p) && Directory.Exists(p);

        public static List<string> GetSteamLibraryPaths()
        {
            var result = new List<string> { SteamPath };
            var vdf = Path.Combine(SteamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                try
                {
                    foreach (Match m in Regex.Matches(
                        File.ReadAllText(vdf), @"""path""\s+""([^""]+)"""))
                    {
                        var lp = m.Groups[1].Value.Replace("\\\\", "\\");
                        if (!result.Contains(lp)) result.Add(lp);
                    }
                }
                catch { }
            }
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType != DriveType.Fixed) continue;
                foreach (var sub in new[] { "Steam", "SteamLibrary", Path.Combine("Games", "Steam") })
                {
                    var p = Path.Combine(drive.Name, sub);
                    if (Directory.Exists(p) && !result.Contains(p)) result.Add(p);
                }
            }
            return result;
        }

        private static string AutoDetectGamePath()
        {
            foreach (var lib in GetSteamLibraryPaths())
            {
                var common = Path.Combine(lib, "steamapps", "common");
                if (!Directory.Exists(common)) continue;
                try
                {
                    foreach (var dir in Directory.GetDirectories(common))
                    {
                        var n = Path.GetFileName(dir);
                        if (n.Equals("EscapeTheBackrooms", StringComparison.OrdinalIgnoreCase) ||
                            n.Equals("Escape the Backrooms", StringComparison.OrdinalIgnoreCase))
                            return dir;
                    }
                }
                catch { }
            }
            return Path.Combine(SteamPath, "steamapps", "common", "EscapeTheBackrooms");
        }

        public static void LoadCustomPaths()
        {
            try
            {
                if (!File.Exists(SettingsFile)) return;
                var r = JsonDocument.Parse(File.ReadAllText(SettingsFile)).RootElement;
                if (r.TryGetProperty("CustomSavePath", out var sp)) _customSavePath = sp.GetString();
                if (r.TryGetProperty("CustomCrashPath", out var cp)) _customCrashPath = cp.GetString();
                if (r.TryGetProperty("CustomGamePath", out var gp)) _customGamePath = gp.GetString();
            }
            catch { }
        }

        public static void SaveCustomPaths()
        {
            try
            {
                var dict = new Dictionary<string, string>();
                if (File.Exists(SettingsFile))
                    foreach (var p in JsonDocument.Parse(File.ReadAllText(SettingsFile))
                        .RootElement.EnumerateObject())
                        if (p.Value.ValueKind == JsonValueKind.String)
                            dict[p.Name] = p.Value.GetString() ?? "";
                dict["CustomSavePath"] = _customSavePath ?? "";
                dict["CustomCrashPath"] = _customCrashPath ?? "";
                dict["CustomGamePath"] = _customGamePath ?? "";
                File.WriteAllText(SettingsFile,
                    JsonSerializer.Serialize(dict,
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static void SetCustomSavePath(string? p) { _customSavePath = p; SaveCustomPaths(); }
        public static void SetCustomCrashPath(string? p) { _customCrashPath = p; SaveCustomPaths(); }
        public static void SetCustomGamePath(string? p) { _customGamePath = p; SaveCustomPaths(); }
        public static string? GetRawCustomSavePath() => _customSavePath;
        public static string? GetRawCustomCrashPath() => _customCrashPath;
        public static string? GetRawCustomGamePath() => _customGamePath;

        static GamePaths()
        {
            if (!Directory.Exists(AppConfigPath))
                Directory.CreateDirectory(AppConfigPath);
            LoadCustomPaths();
        }
    }
}