using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ETBTool.Models;

namespace ETBTool.Utils
{
    public static class UpdateChecker
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30), DefaultRequestHeaders = { { "User-Agent", "ETBTool/0.0.03" } } };
        private static readonly string[] FunnyZH = { "您的网络好像离家出走了", "与 GitHub 服务器沟通失败了", "网络信号去度假了", "服务器需要喝杯咖啡" };
        private static readonly string[] FunnyEN = { "Network went for a walk", "GitHub server unreachable", "Network on vacation", "Server needs coffee" };
        private static readonly Random _rng = new();

        public static async Task<(bool hasUpdate, string latest, string message, string downloadUrl, string mirrorUrl)> CheckAsync()
        {
            try
            {
                string resp = await _http.GetStringAsync(GamePaths.UpdateCheckUrl);
                string latest = TryExtract(resp, "latest_version", "version", "latest", "ver");
                string dlUrl = TryExtract(resp, "download_url", "download", "url", "html_url");
                string miUrl = TryExtract(resp, "mirror_url", "mirror", "cdn_url", "alt_url");
                if (string.IsNullOrEmpty(latest)) return (false, "", LanguageManager.IsChinese ? "无法识别版本信息" : "Cannot read version", "", "");
                if (IsNewerVersion(latest, GamePaths.DisplayVersion))
                {
                    var msg = LanguageManager.IsChinese ? $"发现新版本 v{latest}，当前版本 v{GamePaths.DisplayVersion}" : $"New v{latest} (current v{GamePaths.DisplayVersion})";
                    if (!LanguageManager.IsChinese) miUrl = "";
                    return (true, latest, msg, dlUrl, miUrl);
                }
                return (false, GamePaths.DisplayVersion, LanguageManager.MsgNoUpdate, "", "");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden) { var e = LanguageManager.IsChinese ? FunnyZH : FunnyEN; return (false, "", $"(403) {e[_rng.Next(e.Length)]}", "", ""); }
            catch (HttpRequestException) { var e = LanguageManager.IsChinese ? FunnyZH : FunnyEN; return (false, "", e[_rng.Next(e.Length)], "", ""); }
            catch (TaskCanceledException) { return (false, "", LanguageManager.IsChinese ? "检查超时" : "Timed out", "", ""); }
            catch (Exception ex) { return (false, "", $"{(LanguageManager.IsChinese ? "检查失败" : "Failed")}: {ex.Message}", "", ""); }
        }

        private static string TryExtract(string json, params string[] keys)
        {
            try
            {
                var r = JsonDocument.Parse(json).RootElement; foreach (var k in keys) if (r.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString() ?? "";
                if (r.ValueKind == JsonValueKind.Array && r.GetArrayLength() > 0) foreach (var k in keys) if (r[0].TryGetProperty(k, out var v2) && v2.ValueKind == JsonValueKind.String) return v2.GetString() ?? "";
            }
            catch { }
            return "";
        }

        public static bool IsNewerVersion(string latest, string current) { try { string[] l = latest.Split('.'), c = current.Split('.'); int len = Math.Max(l.Length, c.Length); for (int i = 0; i < len; i++) { int ln = i < l.Length && int.TryParse(l[i], out int a) ? a : 0; int cn = i < c.Length && int.TryParse(c[i], out int b) ? b : 0; if (ln > cn) return true; if (cn > ln) return false; } } catch { } return false; }

        public static void OpenUpdatePage(string? dl = null, string? mi = null) { var url = !string.IsNullOrWhiteSpace(mi) ? mi : !string.IsNullOrWhiteSpace(dl) ? dl : GamePaths.UpdateDownloadUrl; Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
    }
}