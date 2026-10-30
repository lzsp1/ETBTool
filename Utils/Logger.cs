using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using ETBTool.Models;

namespace ETBTool.Utils
{
    public static class Logger
    {
        private static readonly object _lock = new();
        public static event Action<string>? OnLogAdded;

        private static readonly Dictionary<string, string> Messages = new(StringComparer.Ordinal)
        {
            ["ModManagerPage: ctor start"] = "Mod 管理：开始初始化",
            ["ModManagerPage: InitializeComponent OK"] = "Mod 管理：界面组件初始化完成",
            ["ModManagerPage: ItemsSource OK"] = "Mod 管理：列表数据绑定完成",
            ["ModManagerPage: RefreshText OK"] = "Mod 管理：界面文本刷新完成",
            ["ModManagerPage: Loaded start"] = "Mod 管理：开始加载",
            ["ModManagerPage: Translation loaded OK"] = "Mod 管理：翻译数据加载完成",
            ["ModManagerPage: Directory loaded OK"] = "Mod 管理：目录加载完成",
            ["ModManagerPage: ctor end OK"] = "Mod 管理：初始化完成",
            ["App started"] = "应用已启动",
            ["Started"] = "已启动",
            ["Up to date"] = "当前已是最新版本",
            ["Update check timed out"] = "检查更新超时了",
            ["Cannot read version"] = "无法识别版本信息",
            ["Unable to check for updates. Check your connection and retry."] = "暂时无法检查更新，请确认网络连接后重试。",
            ["A task was canceled."] = "任务已取消（可能是请求超时或主动取消）。",
            ["The operation was canceled."] = "操作已取消（可能是请求超时或主动取消）。",
            ["The operation has timed out."] = "操作超时，请检查网络连接后重试。",
            ["The request timed out."] = "请求超时，请检查网络连接后重试。",
            ["An error occurred while sending the request."] = "发送网络请求时发生错误，请检查网络或代理设置。",
            ["The SSL connection could not be established, see inner exception."] = "无法建立 SSL 安全连接，请查看内部异常并检查证书或代理设置。",
            ["Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host."] = "无法从传输连接读取数据：远程主机强制关闭了现有连接。",
            ["Attempted to read past the end of the stream."] = "读取超出数据流末尾，文件可能不完整。",
            ["Object reference not set to an instance of an object."] = "对象引用未设置为对象实例。",
            ["The download API returned an empty response."] = "下载接口返回了空响应。",
            ["The download API did not return a valid download URL."] = "下载接口未返回有效的下载地址。",
            ["Save metadata is too large."] = "存档元数据过大。",
            ["Not a GVAS save."] = "不是有效的 GVAS 存档。",
            ["Unsupported custom-version format."] = "不支持此自定义版本格式。",
            ["SaveDisplayNamesLookup is absent."] = "缺少 SaveDisplayNamesLookup 存档名称映射。",
            ["Invalid property size."] = "属性大小无效。",
            ["Invalid property GUID marker."] = "属性 GUID 标记无效。",
            ["Unsupported display-name map."] = "不支持此存档名称映射格式。",
            ["Duplicate save slot."] = "存档槽位重复。",
            ["Invalid display-name map length."] = "存档名称映射长度无效。",
            ["Too many save properties."] = "存档属性数量过多。",
            ["Invalid collection count."] = "集合元素数量无效。",
            ["Invalid FString length."] = "FString 字符串长度无效。",
            ["Unterminated FString."] = "FString 字符串缺少结束标记。"
        };

        private static readonly (string English, string Chinese, bool Diagnostic)[] Prefixes =
        {
            ("ModManagerPage: ModPath = ", "Mod 管理：目录 = ", false),
            ("ModManagerPage: GamePaths.ModPath failed: ", "Mod 管理：获取目录失败：", true),
            ("ModManagerPage: Loaded FAILED: ", "Mod 管理：加载失败：", true),
            ("ModManagerPage: ctor CRASH: ", "Mod 管理：初始化异常：", true),
            ("LoadMods: ", "加载 Mod 列表失败：", true),
            ("Login: ", "登录失败：", true),
            ("Download: ", "下载失败：", true),
            ("Skip: ", "跳过无效条目：", true),
            ("Background music import failed: ", "背景音乐导入失败：", true),
            ("Background music clear failed: ", "背景音乐清除失败：", true),
            ("Background music volume could not be saved: ", "背景音乐音量保存失败：", true),
            ("Background music playback failed: ", "背景音乐播放失败：", true),
            ("Background music cleanup failed: ", "背景音乐资源清理失败：", true),
            ("Mandatory update state: ", "强制更新状态读取失败：", true),
            ("Update check: ", "更新检查：", true),
            ("Crash log loading failed: ", "崩溃日志加载失败：", true),
            ("Bundled translations: ", "内置翻译加载失败：", true),
            ("Catalogue translations (using cache): ", "在线翻译加载失败（使用缓存）：", true),
            ("Translation cache: ", "翻译缓存读取失败：", true),
            ("RefreshText: ", "刷新界面文本失败：", true),
            ("ScanDir item: ", "扫描子目录失败：", true),
            ("ScanFile item: ", "扫描文件失败：", true),
            ("ScanDir: ", "扫描目录失败：", true),
            ("EntriesAdd: ", "添加列表条目失败：", true),
            ("LoadDir: ", "加载目录失败：", true),
            ("ModDblClick: ", "打开 Mod 条目失败：", true),
            ("Toggle: ", "切换 Mod 启用状态失败：", true),
            ("Nick: ", "修改 Mod 别名失败：", true),
            ("Translation load failed: ", "翻译加载失败：", true),
            ("Read failed: ", "读取失败：", true),
            ("Accent: ", "强调色：", false)
        };

        // Only match whole messages or explicit boundaries, never words inside paths or payloads.
        private static Match Match(string value, string pattern) =>
            Regex.Match(value, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        public static string NormalizeForDisplay(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;
            try
            {
                var timestamp = Match(message, @"\A\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] ");
                string header = timestamp.Value;
                string text = message.Substring(header.Length);
                return header + RedactSecrets(NormalizeMessage(text));
            }
            catch (RegexMatchTimeoutException)
            {
                // Do not emit potentially unredacted credentials if a pathological input times out.
                return "日志内容过长或格式异常，已隐藏以保护认证信息。";
            }
        }

        private static string NormalizeMessage(string text)
        {
            if (Messages.TryGetValue(text, out var translated)) return translated;
            foreach (var prefix in Prefixes)
            {
                if (!text.StartsWith(prefix.English, StringComparison.Ordinal)) continue;
                string detail = text.Substring(prefix.English.Length);
                return prefix.Chinese + (prefix.Diagnostic ? NormalizeDiagnostic(detail) : detail);
            }
            var loaded = Match(text, @"\ALoaded (?<count>\d+) (?<kind>items|saves|translations)\z");
            if (loaded.Success)
            {
                string kind = loaded.Groups["kind"].Value;
                return "已加载 " + loaded.Groups["count"].Value + (kind == "saves" ? " 个存档" : kind == "translations" ? " 条翻译" : " 项");
            }
            var json = Match(text, @"\AJSON \((?<endpoint>[^\r\n]*)\): (?<detail>[^\r\n]*)\z");
            if (json.Success)
                return "JSON 响应解析失败（" + json.Groups["endpoint"].Value + "）：" + NormalizeDiagnostic(json.Groups["detail"].Value);
            var save = Match(text, @"\A\[UE4SaveReader\] (?<file>[^\r\n]*?): (?<detail>[^\r\n]*)\z");
            if (save.Success)
                return "存档读取失败 [UE4SaveReader] " + save.Groups["file"].Value + ": " + NormalizeDiagnostic(save.Groups["detail"].Value);
            // Existing Chinese operational prefixes often carry an English exception message.
            var chinese = Match(text, @"\A(?<prefix>[\p{IsCJKUnifiedIdeographs} A-Za-z0-9_（）]+(?:失败|异常|检查): )(?<detail>[^\r\n]*)\z");
            if (chinese.Success)
                return chinese.Groups["prefix"].Value + NormalizeDiagnostic(chinese.Groups["detail"].Value);
            return NormalizeDiagnostic(text);
        }

        private static string NormalizeDiagnostic(string text)
        {
            if (Messages.TryGetValue(text, out var translated)) return translated;
            // Keep exception types, HRESULTs, inner exceptions and stack traces for troubleshooting.
            var exception = Match(text, @"\A(?<type>(?:System\.)[A-Za-z0-9_.`]+Exception(?: \(0x[0-9A-Fa-f]+\))?: )(?<detail>[^\r\n]*)(?<stack>[\r\n][\s\S]*)?\z");
            if (exception.Success)
                return exception.Groups["type"].Value + NormalizeDiagnostic(exception.Groups["detail"].Value) + exception.Groups["stack"].Value;
            var status = Match(text, @"\AResponse status code does not indicate success: (?<code>\d{3}) \((?<reason>[^\r\n()]*)\)\.(?<tail>\s[\s\S]*)?\z");
            if (status.Success)
            {
                string code = status.Groups["code"].Value;
                string hint = code switch
                {
                    "400" => "请求参数无效", "401" => "身份验证失败，请重新登录",
                    "403" => "服务器拒绝访问，请检查账号权限或下载限制", "404" => "请求的资源不存在",
                    "408" => "服务器等待请求超时", "429" => "请求过于频繁，请稍后重试",
                    "500" => "服务器内部错误", "502" => "网关错误", "503" => "服务暂不可用", "504" => "网关超时",
                    _ => "服务器返回非成功状态"
                };
                return "HTTP 请求失败：" + code + " (" + status.Groups["reason"].Value + ")，" + hint + "。" + status.Groups["tail"].Value;
            }
            var timeout = Match(text, @"\AThe request was canceled due to the configured HttpClient\.Timeout of (?<seconds>[0-9.]+) seconds elapsing\.\z");
            if (timeout.Success) return "网络请求超时：已超过 HttpClient.Timeout 设置的 " + timeout.Groups["seconds"].Value + " 秒，请检查网络连接后重试。";
            var file = Match(text, @"\ACould not find file '(?<path>[^\r\n]*)'\.\z");
            if (file.Success) return "找不到文件 '" + file.Groups["path"].Value + "'。";
            var directory = Match(text, @"\ACould not find a part of the path '(?<path>[^\r\n]*)'\.\z");
            if (directory.Success) return "找不到路径的一部分 '" + directory.Groups["path"].Value + "'。";
            var denied = Match(text, @"\AAccess to the path '(?<path>[^\r\n]*)' is denied\.\z");
            if (denied.Success) return "访问路径被拒绝 '" + denied.Groups["path"].Value + "'，请检查权限或文件占用。";
            var busy = Match(text, @"\AThe process cannot access the file '(?<path>[^\r\n]*)' because it is being used by another process\.\z");
            if (busy.Success) return "无法访问文件 '" + busy.Groups["path"].Value + "'，该文件正被其他进程使用。";
            var host = Match(text, @"\ANo such host is known\.(?<host> \([^\r\n]*\))?\z");
            if (host.Success) return "无法解析服务器地址，请检查网络或 DNS 设置。" + host.Groups["host"].Value;
            var version = Match(text, @"\AUnsupported GVAS version (?<version>-?\d+)\.\z");
            if (version.Success) return "不支持 GVAS 存档版本 " + version.Groups["version"].Value + "。";
            var property = Match(text, @"\AUnsupported property type (?<type>[A-Za-z0-9_]+)\.\z");
            if (property.Success) return "不支持属性类型 " + property.Groups["type"].Value + "。";
            var json = Match(text, @"\AThe JSON value could not be converted to (?<type>[^\r\n]+?)\. Path: (?<location>[^\r\n]*)\z");
            if (json.Success) return "JSON 值无法转换为 " + json.Groups["type"].Value + "。位置: " + json.Groups["location"].Value;
            var invalidJson = Match(text, @"\A'(?<character>[^\r\n]*)' is an invalid start of a value\.(?<location>[^\r\n]*)\z");
            if (invalidJson.Success) return "JSON 值的起始字符无效：'" + invalidJson.Groups["character"].Value + "'。" + invalidJson.Groups["location"].Value;
            var empty = Match(text, @"\AThe input does not contain any JSON tokens\. Expected the input to start with a valid JSON token, when isFinalBlock is true\.(?<location>[^\r\n]*)\z");
            if (empty.Success) return "输入中没有 JSON 数据，预期为有效的 JSON 内容。" + empty.Groups["location"].Value;
            return text;
        }

        private static string RedactSecrets(string text)
        {
            const RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
            var timeout = TimeSpan.FromMilliseconds(100);
            text = Regex.Replace(text, @"(\bAuthorization\s*:\s*(?:Bearer|Basic)\s+)[^\s,;""'<>]+", "$1[已隐藏]", options, timeout);
            text = Regex.Replace(text, @"((?:\A|[\r\n]|:\s|：)Bearer\s+)[A-Za-z0-9._~+/=-]+", "$1[已隐藏]", options, timeout);
            text = Regex.Replace(text, @"([?&](?:access_token|refresh_token|id_token|token|api_key|apikey|password)=)[^&\s#""'<>]*", "$1[已隐藏]", options, timeout);
            return Regex.Replace(text, @"(""(?:access_token|refresh_token|id_token|token|api_key|apikey|password)""\s*:\s*"")(?:\\.|[^""\\])*""", "$1[已隐藏]\"", options, timeout);
        }

        public static void LogException(string operation, Exception exception)
        {
            Log(operation + ": " + exception);
        }

        public static void Log(string message)
        {
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {NormalizeForDisplay(message)}";
            lock (_lock)
            {
                try { File.AppendAllText(GamePaths.LogFilePath, entry + Environment.NewLine); }
                catch { }
            }
            OnLogAdded?.Invoke(entry);
        }

        public static string[] ReadAll()
        {
            try
            {
                if (File.Exists(GamePaths.LogFilePath))
                    return File.ReadAllLines(GamePaths.LogFilePath);
            }
            catch { }
            return Array.Empty<string>();
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(GamePaths.LogFilePath))
                    File.WriteAllText(GamePaths.LogFilePath, "");
            }
            catch { }
            Log("日志已清空");
        }

        /// <summary>最简剪贴板复制</summary>
        public static bool SafeSetClipboard(string text)
        {
            try { Clipboard.SetText(text); return true; }
            catch { return false; }
        }
    }
}