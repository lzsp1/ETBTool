using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using ETBTool.Models;

namespace ETBTool.Utils
{
    public static class UpdateChecker
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(30),
            DefaultRequestHeaders = { { "User-Agent", "ETBTool/0.0.01" } }
        };

        private static readonly string[] FunnyNetworkErrors =
        {
            "您的网络好像离家出走了，请稍后再试",
            "与 GitHub 服务器沟通失败了，它可能在休息",
            "GitHub 说我们太热情，它有点承受不住",
            "网络信号去度假了，连接被中断",
            "服务器表示需要喝杯咖啡，请稍后重试",
            "连接被拒，可能是网络社恐了"
        };

        private static readonly Random _rng = new();

        public static async Task<(bool hasUpdate, string latest, string message, string downloadUrl, string mirrorUrl)> CheckAsync()
        {
            try
            {
                string response = await _http.GetStringAsync(GamePaths.UpdateCheckUrl);
                string latest = TryExtractString(response, "latest_version", "version", "latest", "ver", "Version", "Latest");
                string downloadUrl = TryExtractString(response, "download_url", "download", "url", "html_url");
                string mirrorUrl = TryExtractString(response, "mirror_url", "mirror", "cdn_url", "alt_url");

                if (string.IsNullOrEmpty(latest))
                    return (false, "", "无法识别版本信息，请稍后重试", "", "");

                if (IsNewerVersion(latest, GamePaths.DisplayVersion))
                    return (true, latest,
                        $"发现新版本 v{latest}，当前版本 v{GamePaths.DisplayVersion}",
                        downloadUrl, mirrorUrl);

                return (false, GamePaths.DisplayVersion, "当前已是最新版本", "", "");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                var msg = FunnyNetworkErrors[_rng.Next(FunnyNetworkErrors.Length)];
                return (false, "", $"(403) {msg}", "", "");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, "", "更新信息文件不存在，可能仓库地址已变更", "", "");
            }
            catch (HttpRequestException)
            {
                var msg = FunnyNetworkErrors[_rng.Next(FunnyNetworkErrors.Length)];
                return (false, "", msg, "", "");
            }
            catch (TaskCanceledException)
            {
                return (false, "", "检查更新超时了，网络可能有点慢", "", "");
            }
            catch (Exception ex)
            {
                return (false, "", $"检查更新时出了点意外: {ex.Message}", "", "");
            }
        }

        private static string TryExtractString(string json, params string[] keys)
        {
            try
            {
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                foreach (string k in keys)
                    if (root.TryGetProperty(k, out var v) &&
                        v.ValueKind == JsonValueKind.String)
                        return v.GetString() ?? "";
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    foreach (string k in keys)
                        if (root[0].TryGetProperty(k, out var v2) &&
                            v2.ValueKind == JsonValueKind.String)
                            return v2.GetString() ?? "";
            }
            catch { }
            return "";
        }

        public static bool IsNewerVersion(string latest, string current)
        {
            try
            {
                string[] l = latest.Split('.');
                string[] c = current.Split('.');
                int len = Math.Max(l.Length, c.Length);
                for (int i = 0; i < len; i++)
                {
                    int ln = i < l.Length && int.TryParse(l[i], out int a) ? a : 0;
                    int cn = i < c.Length && int.TryParse(c[i], out int b) ? b : 0;
                    if (ln > cn) return true;
                    if (cn > ln) return false;
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 打开下载页面：优先镜像，失败后回退原链接
        /// </summary>
        public static void OpenUpdatePage(string? downloadUrl = null, string? mirrorUrl = null)
        {
            // 优先用镜像链接
            var url = !string.IsNullOrWhiteSpace(mirrorUrl) ? mirrorUrl
                    : !string.IsNullOrWhiteSpace(downloadUrl) ? downloadUrl
                    : GamePaths.UpdateDownloadUrl;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        /// <summary>
        /// 直接下载更新包到桌面，优先镜像链接
        /// </summary>
        public static async Task<bool> DownloadUpdateAsync(
            string? downloadUrl, string? mirrorUrl, IProgress<int>? progress = null)
        {
            // 构建候选链接列表：镜像优先，原链接兜底
            var candidates = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrWhiteSpace(mirrorUrl)) candidates.Add(mirrorUrl);
            if (!string.IsNullOrWhiteSpace(downloadUrl)) candidates.Add(downloadUrl);

            if (candidates.Count == 0)
            {
                Toast.Show("没有可用的下载链接", ToastType.Error);
                return false;
            }

            foreach (var url in candidates)
            {
                try
                {
                    Logger.Log($"尝试下载: {url}");
                    using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
                    var savePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                    await using var stream = await response.Content.ReadAsStreamAsync();
                    await using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    var buffer = new byte[81920];
                    long totalRead = 0;
                    int read;
                    while ((read = await stream.ReadAsync(buffer)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, read));
                        totalRead += read;
                        if (totalBytes > 0)
                            progress?.Report((int)(totalRead * 100 / totalBytes));
                    }

                    Logger.Log($"下载完成: {savePath}");
                    Toast.Show($"已下载到桌面: {fileName}", ToastType.Success);

                    // 打开所在文件夹
                    Process.Start("explorer.exe", $"/select,\"{savePath}\"");
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"下载失败 ({url}): {ex.Message}");
                }
            }

            Toast.Show("所有下载源均失败，请手动前往 GitHub 下载", ToastType.Error);
            // 最后兜底：打开 GitHub releases 页面
            Process.Start(new ProcessStartInfo(GamePaths.UpdateDownloadUrl) { UseShellExecute = true });
            return false;
        }
    }
}