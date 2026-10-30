using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ETBTool.Models;
using ETBTool.Services;

namespace ETBTool.Utils
{
    public sealed class UpdateResult
    {
        [JsonPropertyName("has_update")] public bool hasUpdate { get; set; }
        [JsonPropertyName("version")] public string latest { get; set; } = "";
        [JsonIgnore] public string message { get; set; } = "";
        [JsonPropertyName("changelog")] public string changelog { get; set; } = "";
        [JsonPropertyName("force_update")] public bool forceUpdate { get; set; }
        [JsonPropertyName("url")] public string downloadUrl { get; set; } = "";
        [JsonPropertyName("file_name")] public string fileName { get; set; } = "";
        [JsonPropertyName("file_size")] public long fileSize { get; set; }
        [JsonPropertyName("file_etag")] public string fileEtag { get; set; } = "";
        [JsonIgnore] public bool Succeeded { get; set; }
    }

    public static class UpdateChecker
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
        private static string RequirementPath => Path.Combine(GamePaths.AppConfigPath, "mandatory-update.json");
        private static readonly object StateLock = new();
        private static string _observedRequirement = "";
        private const long MaximumPackageSize = 2L * 1024 * 1024 * 1024;
        private static readonly byte[] BundleSignature = Convert.FromHexString("8B1202B96A612038727B930214D7A03213F5B9E6EFAE3318EE3B2DCE24B36AAE");

        public sealed class MandatoryRequirement
        {
            public string Version { get; set; } = "";
        }

        // An unreadable existing requirement fails closed, instead of allowing an offline bypass.
        internal static string ReadRequirement(string path, string current)
        {
            try
            {
                using var stream = File.OpenRead(path);
                var state = JsonSerializer.Deserialize<MandatoryRequirement>(stream);
                if (state == null || !IsValidVersion(state.Version)) throw new InvalidDataException("强制更新状态损坏，请联网重新检查更新。");
                return IsNewerVersion(state.Version, current) ? state.Version : "";
            }
            catch (FileNotFoundException) { return ""; }
            catch (DirectoryNotFoundException) { return ""; }
        }

        public static bool HasMandatoryUpdate
        {
            get
            {
                lock (StateLock)
                {
                    try { return IsNewerVersion(_observedRequirement, GamePaths.DisplayVersion) || ReadRequirement(RequirementPath, GamePaths.DisplayVersion).Length > 0; }
                    catch { return true; }
                }
            }
        }

        public static Task<UpdateResult> CheckAsync(CancellationToken ct = default) =>
            CheckAsync(Http, GamePaths.UpdateCheckUrl, GamePaths.DisplayVersion, RequirementPath, ct);

        internal static async Task<UpdateResult> CheckAsync(HttpClient client, string endpoint, string current, string statePath, CancellationToken ct = default)
        {
            var required = "";
            var stateInvalid = false;
            try { required = ReadRequirement(statePath, current); }
            catch (Exception ex) { stateInvalid = true; Logger.Log($"Mandatory update state: {ex.Message}"); }
            try
            {
                var url = endpoint + (endpoint.Contains('?') ? "&" : "?") + "current=" + Uri.EscapeDataString(current);
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
                var raw = await response.Content.ReadAsStringAsync(ct);
                using var json = JsonDocument.Parse(raw);
                if (json.RootElement.ValueKind != JsonValueKind.Object ||
                    !json.RootElement.TryGetProperty("has_update", out var available) ||
                    available.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    throw new InvalidDataException("更新服务器返回了无效数据。");
                if (available.GetBoolean() && (!json.RootElement.TryGetProperty("force_update", out var forced) ||
                    forced.ValueKind is not (JsonValueKind.True or JsonValueKind.False)))
                    throw new InvalidDataException("更新服务器未提供强制更新状态。");
                var result = JsonSerializer.Deserialize<UpdateResult>(raw) ?? throw new InvalidDataException("更新数据为空。");
                if (!result.hasUpdate && string.IsNullOrEmpty(result.latest)) result.latest = current;
                if (!IsValidVersion(result.latest)) throw new InvalidDataException("更新版本号无效。");
                result.hasUpdate = result.hasUpdate && IsNewerVersion(result.latest, current);
                if (result.hasUpdate && result.forceUpdate)
                {
                    if (!IsNewerVersion(required, result.latest)) required = result.latest;
                    lock (StateLock)
                    {
                        if (statePath == RequirementPath) _observedRequirement = required;
                        ConfigPersistence.Write(statePath, new MandatoryRequirement { Version = required });
                    }
                    stateInvalid = false;
                }
                if (stateInvalid) throw new InvalidDataException("无法确认已保存的强制更新要求，请联系管理员。");
                if (required.Length > 0)
                {
                    result.forceUpdate = true;
                    if (!result.hasUpdate || IsNewerVersion(required, result.latest))
                        throw new InvalidDataException($"仍需更新至 v{required} 或更高版本，服务器暂未提供可用更新。");
                }
                if (result.hasUpdate) ValidateDownloadMetadata(result);
                result.Succeeded = true;
                result.message = result.hasUpdate
                    ? (LanguageManager.IsChinese ? $"发现新版本 v{result.latest}，当前版本 v{current}" : $"New v{result.latest} (current v{current})")
                    : LanguageManager.MsgNoUpdate;
                return result;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                Logger.Log($"Update check: {ex.Message}");
                return new UpdateResult
                {
                    forceUpdate = stateInvalid || required.Length > 0,
                    message = LanguageManager.IsChinese ? "暂时无法检查更新，请确认网络连接后重试。" : "Unable to check for updates. Check your connection and retry."
                };
            }
        }

        internal static bool IsValidVersion(string? value) => value != null &&
            Regex.IsMatch(value, @"^[vV]?\d+(\.\d+){1,3}$", RegexOptions.CultureInvariant) &&
            value.TrimStart('v', 'V').Split('.').All(x => int.TryParse(x, out _));

        public static bool IsNewerVersion(string latest, string current)
        {
            if (!IsValidVersion(latest) || !IsValidVersion(current)) return false;
            var l = latest.TrimStart('v', 'V').Split('.').Select(int.Parse).ToArray();
            var c = current.TrimStart('v', 'V').Split('.').Select(int.Parse).ToArray();
            for (var i = 0; i < Math.Max(l.Length, c.Length); i++)
            {
                var a = i < l.Length ? l[i] : 0;
                var b = i < c.Length ? c[i] : 0;
                if (a != b) return a > b;
            }
            return false;
        }

        internal static void ValidateDownloadMetadata(UpdateResult update)
        {
            if (!Uri.TryCreate(update.downloadUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
                throw new InvalidDataException("服务器未返回有效的 HTTPS 更新下载地址。");
            if (!Path.GetExtension(update.fileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("更新文件必须是 ZIP 压缩包。");
            if (update.fileSize <= 0 || update.fileSize > MaximumPackageSize)
                throw new InvalidDataException("更新包大小无效。");
            if (!Regex.IsMatch(update.fileEtag.Trim('"'), "^[a-fA-F0-9]{32}$", RegexOptions.CultureInvariant))
                throw new InvalidDataException("服务器未提供有效的更新文件 MD5 校验值。");
        }

        /// <summary>更新包下载失败时，判断是否为风控导致的 404。</summary>
        public static bool LooksLikeRiskControl(Exception ex)
        {
            if (ex is HttpRequestException http && http.StatusCode == System.Net.HttpStatusCode.NotFound) return true;
            var message = ex.Message ?? "";
            return message.Contains("404") || message.Contains("Not Found") || message.Contains("请求的资源不存在");
        }

        /// <summary>向接口单独索取直链（失败时返回 null，不抛异常）。</summary>
        public static async Task<string?> GetDirectUpdateUrlAsync(CancellationToken ct = default)
        {
            try
            {
                var url = GamePaths.UpdateCheckUrl + (GamePaths.UpdateCheckUrl.Contains('?') ? "&" : "?")
                    + "current=" + Uri.EscapeDataString(GamePaths.DisplayVersion) + "&mode=direct";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var resp = await Http.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode) return null;
                var result = JsonSerializer.Deserialize<UpdateResult>(await resp.Content.ReadAsStringAsync(ct));
                return result?.hasUpdate == true && !string.IsNullOrWhiteSpace(result.downloadUrl) ? result.downloadUrl : null;
            }
            catch (Exception ex)
            {
                Logger.Log($"获取直链更新地址失败：{ex.Message}");
                return null;
            }
        }

        /// <summary>用用户自己下载好的更新包完成安装。</summary>
        public static async Task InstallFromPackageAsync(string packagePath, UpdateResult update, CancellationToken ct = default)
        {
            if (!File.Exists(packagePath)) throw new FileNotFoundException("找不到选择的更新包。", packagePath);
            if (!string.Equals(Path.GetExtension(packagePath), ".zip", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("更新包必须是 ZIP 文件。");
            var packageDir = Path.Combine(GamePaths.AppConfigPath, "updates", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(packageDir);
            var localCopy = Path.Combine(packageDir, "package.zip");
            Logger.Log($"开始安装本地更新包：{packagePath}（{new FileInfo(packagePath).Length} 字节）");
            File.Copy(packagePath, localCopy, true);
            var manifest = await Task.Run(() => PreparePackage(localCopy, Path.Combine(packageDir, "stage"), update, ct), ct);
            ct.ThrowIfCancellationRequested();
            await LaunchInstallerAsync(packageDir, AppContext.BaseDirectory, manifest, ct);
        }

        public static async Task DownloadAndInstallAsync(UpdateResult update, IProgress<DownloadProgress>? progress = null, CancellationToken ct = default)
        {
            ValidateDownloadMetadata(update);
            var packageDir = Path.Combine(GamePaths.AppConfigPath, "updates", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(packageDir);
            var packagePath = Path.Combine(packageDir, "package.zip");
            using var req = new HttpRequestMessage(HttpMethod.Get, update.downloadUrl);
            using var response = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri?.Scheme != Uri.UriSchemeHttps)
                throw new InvalidDataException("更新下载不能重定向到非 HTTPS 地址。");
            if (response.Content.Headers.ContentLength is long length && length != update.fileSize)
                throw new InvalidDataException("更新包大小与服务器记录不符，请重新检查更新。");
            await using (var input = await response.Content.ReadAsStreamAsync(ct))
            await using (var output = new FileStream(packagePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920]; long received = 0; int read; var last = DateTime.UtcNow;
                while ((read = await input.ReadAsync(buffer, ct)) > 0)
                {
                    received += read;
                    if (received > update.fileSize) throw new InvalidDataException("更新下载超出预期大小。");
                    await output.WriteAsync(buffer.AsMemory(0, read), ct);
                    if ((DateTime.UtcNow - last).TotalMilliseconds >= 150)
                    {
                        last = DateTime.UtcNow;
                        progress?.Report(new DownloadProgress { Percent = received * 100f / update.fileSize, BytesReceived = received, TotalBytes = update.fileSize, StatusText = FormatBytes(received) });
                    }
                }
            }
            ct.ThrowIfCancellationRequested();
            progress?.Report(new DownloadProgress { Percent = -1, StatusText = "正在校验更新包..." });
            var manifest = await Task.Run(() => PreparePackage(packagePath, Path.Combine(packageDir, "stage"), update, ct), ct);
            ct.ThrowIfCancellationRequested();
            await LaunchInstallerAsync(packageDir, AppContext.BaseDirectory, manifest, ct);
        }

        internal sealed class StagedFile
        {
            public string Path { get; set; } = "";
            public string Sha256 { get; set; } = "";
        }

        internal static List<StagedFile> PreparePackage(string packagePath, string stage, UpdateResult update, CancellationToken ct = default)
        {
            ValidateDownloadMetadata(update);
            if (new FileInfo(packagePath).Length != update.fileSize) throw new InvalidDataException("更新包大小校验失败。");
            using (var verify = File.OpenRead(packagePath))
            {
                var actual = Convert.ToHexString(MD5.HashData(verify));
                if (!string.Equals(actual, update.fileEtag.Trim('"'), StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("更新文件 MD5 校验失败，已取消安装。");
            }
            if (Directory.Exists(stage)) throw new InvalidDataException("更新暂存目录已存在。");
            ZipArchive archive;
            try { archive = ZipFile.OpenRead(packagePath); }
            catch (InvalidDataException)
            {
                Logger.Log($"更新包不是有效的 ZIP：{packagePath}（{new FileInfo(packagePath).Length} 字节）");
                throw new InvalidDataException("所选文件不是有效的 ZIP 压缩包，可能下载未完成或下载到的是网页，请重新下载后再选择。");
            }
            using var _archiveScope = archive;
            if (archive.Entries.Count > 10000) throw new InvalidDataException("更新包文件数量过多。");
            var rawNames = archive.Entries.Select(e => e.FullName.Replace('\\', '/')).ToList();
            var prefix = DetectPackagePrefix(rawNames);
            Logger.Log($"更新包检查：{packagePath}，共 {rawNames.Count} 个条目，安装根前缀=\"{prefix}\"");
            Logger.Log("更新包前 10 个条目：" + string.Join(" | ", rawNames.Take(10)));
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long expanded = 0;
            foreach (var entry in archive.Entries)
            {
                ct.ThrowIfCancellationRequested();
                var rawName = entry.FullName.Replace('\\', '/');
                if (prefix.Length > 0)
                {
                    if (!rawName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                    // 压缩工具常会写入“外层文件夹”这一条目录记录，跳过它本身。
                    if (rawName.TrimEnd('/').Equals(prefix.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)) continue;
                }
                string name;
                try { name = ValidateEntry(entry, prefix); }
                catch (InvalidDataException ex)
                {
                    Logger.Log($"更新包条目无效：{rawName}（长度 {rawName.Length}）");
                    Logger.Log("更新包条目字符码：" + string.Join(",", rawName.Select(c => ((int)c).ToString())));
                    throw new InvalidDataException(ex.Message, ex);
                }
                if (name.Length == 0) continue;
                if (!names.Add(name.TrimEnd('/'))) throw new InvalidDataException("更新包含重复路径。");
                expanded = checked(expanded + entry.Length);
                if (expanded > MaximumPackageSize) throw new InvalidDataException("更新包解压大小超出限制。");
            }
            if (!names.Contains("ETBTool.exe")) throw new InvalidDataException("更新包结构不正确：第一层没有找到 ETBTool.exe。\n\n请打开压缩包确认：第一层应直接看到 ETBTool.exe；如果多套了一层文件夹，请去掉最外层文件夹后重新压缩。");
            Directory.CreateDirectory(stage);
            var manifest = new List<StagedFile>();
            foreach (var entry in archive.Entries)
            {
                ct.ThrowIfCancellationRequested();
                var rawName = entry.FullName.Replace('\\', '/');
                if (prefix.Length > 0)
                {
                    if (!rawName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                    if (rawName.TrimEnd('/').Equals(prefix.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)) continue;
                }
                var name = StripPackagePrefix(rawName, prefix);
                if (name.Length == 0) continue;
                var destination = Path.GetFullPath(Path.Combine(stage, name));
                if (!destination.StartsWith(Path.GetFullPath(stage) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("更新包路径超出暂存目录。");
                if (name.EndsWith('/')) { Directory.CreateDirectory(destination); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                entry.ExtractToFile(destination, false);
                using var stream = File.OpenRead(destination);
                manifest.Add(new StagedFile { Path = name, Sha256 = Convert.ToHexString(SHA256.HashData(stream)) });
            }
            var exe = Path.Combine(stage, "ETBTool.exe");
            ValidatePortableExecutable(exe);
            var dll = Path.Combine(stage, "ETBTool.dll");
            if (File.Exists(dll)) ValidatePortableExecutable(dll);
            else if (!ContainsBundleSignature(exe)) throw new InvalidDataException("更新包缺少 ETBTool.dll，且 ETBTool.exe 不是 .NET 单文件发布包。");
            var diagnostics = Path.Combine(stage, "createdump.exe");
            if (File.Exists(diagnostics)) ValidatePortableExecutable(diagnostics);
            return manifest;
        }

        /// <summary>找到压缩包内 ETBTool.exe 所在的目录，作为安装根；支持外面套任意层文件夹。</summary>
        internal static string DetectPackagePrefix(IReadOnlyList<string> names)
        {
            var best = "";
            var bestDepth = int.MaxValue;
            foreach (var raw in names)
            {
                var name = raw.Replace('\\', '/').TrimEnd('/');
                if (!name.Equals("ETBTool.exe", StringComparison.OrdinalIgnoreCase) &&
                    !name.EndsWith("/ETBTool.exe", StringComparison.OrdinalIgnoreCase)) continue;
                var index = name.LastIndexOf('/');
                var prefix = index < 0 ? "" : name.Substring(0, index + 1);
                var depth = prefix.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
                if (depth < bestDepth) { best = prefix; bestDepth = depth; }
            }
            return best;
        }

        internal static string StripPackagePrefix(string name, string prefix)
            => prefix.Length > 0 && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? name.Substring(prefix.Length) : name;

        internal static string ValidateEntry(ZipArchiveEntry entry, string prefix = "")
        {
            var name = StripPackagePrefix(entry.FullName.Replace('\\', '/'), prefix);
            var parts = name.TrimEnd('/').Split('/');
            if (name.Length == 0 || name.StartsWith('/') || parts.Any(p => p.Length == 0 || p is "." or ".." ||
                p.EndsWith('.') || p.EndsWith(' ') || p.Any(c => c < 32 || "<>:\"|?*".Contains(c)) ||
                Regex.IsMatch(p, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])($|\.)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)))
                throw new InvalidDataException($"更新包包含非法路径：{name}\n\n请检查压缩包内是否有异常文件名（如空目录名、结尾的点或空格、保留设备名）。");
            var mode = (entry.ExternalAttributes >> 16) & 0xF000;
            if ((mode != 0 && mode != 0x8000 && mode != 0x4000) || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("更新包不允许链接或特殊文件。");
            if ((mode == 0x4000 || (entry.ExternalAttributes & (int)FileAttributes.Directory) != 0) && !name.EndsWith('/'))
                throw new InvalidDataException("更新包目录属性无效。");
            var extension = Path.GetExtension(name);
            if ((extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
                 !name.Equals("ETBTool.exe", StringComparison.OrdinalIgnoreCase) &&
                 !name.Equals("createdump.exe", StringComparison.OrdinalIgnoreCase)) ||
                new[] { ".cmd", ".bat", ".ps1", ".vbs", ".js", ".msi", ".com", ".scr", ".lnk" }.Contains(extension, StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException($"更新包包含非预期的可执行文件：{name}\n\n常见原因是压缩时多套了一层文件夹，或把解压工具等程序一起打了进去。\n请检查压缩包：\n1. 打开后第一层应能直接看到 ETBTool.exe；\n2. 如果外面还套着文件夹，请去掉最外层文件夹后重新压缩；\n3. 压缩包里只保留程序文件，不要带其它 .exe。");
            if (new[] { "settings.json", "notice.json", "mandatory-update.json", "updates", "config", "logs" }.Contains(parts[0], StringComparer.OrdinalIgnoreCase) ||
                (extension.Equals(".json", StringComparison.OrdinalIgnoreCase) && !name.Equals("ETBTool.deps.json", StringComparison.OrdinalIgnoreCase) && !name.Equals("ETBTool.runtimeconfig.json", StringComparison.OrdinalIgnoreCase)) ||
                extension.Equals(".config", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("更新包不得覆盖用户配置。");
            return name;
        }

        private static void ValidatePortableExecutable(string path)
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);
            if (stream.Length < 64 || reader.ReadUInt16() != 0x5A4D) throw new InvalidDataException("更新程序不是有效的 Windows 程序。");
            stream.Position = 0x3C;
            var pe = reader.ReadInt32();
            if (pe < 64 || pe > stream.Length - 4) throw new InvalidDataException("更新程序 PE 头无效。");
            stream.Position = pe;
            if (reader.ReadUInt32() != 0x00004550) throw new InvalidDataException("更新程序 PE 签名无效。");
        }

        private static bool ContainsBundleSignature(string path)
        {
            using var input = File.OpenRead(path);
            var buffer = new byte[81920 + BundleSignature.Length];
            var retained = 0;
            int count;
            while ((count = input.Read(buffer, retained, buffer.Length - retained)) > 0)
            {
                var used = retained + count;
                if (buffer.AsSpan(0, used).IndexOf(BundleSignature) >= 0) return true;
                retained = Math.Min(BundleSignature.Length - 1, used);
                Buffer.BlockCopy(buffer, used - retained, buffer, 0, retained);
            }
            return false;
        }

        internal static void WriteInstallerFiles(string packageDir, string target, List<StagedFile> manifest, int processId, long processStartTicks)
        {
            var settings = new { Target = Path.GetFullPath(target), ProcessId = processId, ProcessStartTicks = processStartTicks, Files = manifest };
            File.WriteAllText(Path.Combine(packageDir, "apply.json"), JsonSerializer.Serialize(settings), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(packageDir, "apply-update.ps1"), InstallerScript, new UTF8Encoding(true));
        }

        private static async Task LaunchInstallerAsync(string packageDir, string target, List<StagedFile> manifest, CancellationToken ct)
        {
            using var current = Process.GetCurrentProcess();
            WriteInstallerFiles(packageDir, target, manifest, current.Id, current.StartTime.ToUniversalTime().Ticks);
            var script = Path.Combine(packageDir, "apply-update.ps1");
            var command = "& '" + script.Replace("'", "''") + "'";
            var start = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"),
                UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = packageDir
            };
            foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(command)) }) start.ArgumentList.Add(arg);
            using var process = Process.Start(start) ?? throw new IOException("无法启动更新安装程序。");
            try
            {
                var ready = Path.Combine(packageDir, "installer-ready");
                var deadline = DateTime.UtcNow.AddSeconds(30);
                while (!File.Exists(ready))
                {
                    ct.ThrowIfCancellationRequested();
                    if (process.HasExited) throw new IOException("更新安装程序未能启动。请检查 Windows PowerShell 策略及更新日志：" + packageDir);
                    if (DateTime.UtcNow >= deadline) throw new IOException("更新安装程序启动超时，当前版本未被修改。更新日志：" + packageDir);
                    await Task.Delay(100, ct);
                }
                ct.ThrowIfCancellationRequested();
            }
            catch
            {
                if (!process.HasExited) process.Kill();
                throw;
            }
        }

        // Paths and file hashes are data in UTF-8 JSON, never interpolated into executable script text.
        internal const string InstallerScript = """
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$backup = Join-Path $root 'backup'
$applied = New-Object System.Collections.Generic.List[object]
$log = Join-Path $root 'install-result.txt'
function Assert-NoLink([string]$path) {
    $cursor = [IO.Path]::GetFullPath($path)
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            if (((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Linked paths are not allowed.' }
        }
        $parent = [IO.Path]::GetDirectoryName($cursor)
        if ($parent -eq $cursor) { break }
        $cursor = $parent
    }
}
try {
    $data = Get-Content -LiteralPath (Join-Path $root 'apply.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $target = [IO.Path]::GetFullPath($data.Target).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $stage = Join-Path $root 'stage'
    Assert-NoLink $root
    Assert-NoLink $target
    Assert-NoLink $backup
    foreach ($file in $data.Files) {
        $source = [IO.Path]::GetFullPath((Join-Path $stage $file.Path))
        $destination = [IO.Path]::GetFullPath((Join-Path $target $file.Path))
        if (!$destination.StartsWith($target, [StringComparison]::OrdinalIgnoreCase) -or !$source.StartsWith($stage + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid staged path.' }
        Assert-NoLink $source
        Assert-NoLink $destination
        if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $file.Sha256) { throw 'Staged update verification failed.' }
    }
    [IO.File]::WriteAllText((Join-Path $root 'installer-ready'), 'ready', [Text.Encoding]::UTF8)
    $deadline = [DateTime]::UtcNow.AddSeconds(120)
    while ($true) {
        $running = Get-Process -Id $data.ProcessId -ErrorAction SilentlyContinue
        if (!$running -or $running.StartTime.ToUniversalTime().Ticks -ne $data.ProcessStartTicks) { break }
        if ([DateTime]::UtcNow -ge $deadline) { throw 'ETBTool did not exit. No update files were installed.' }
        Start-Sleep -Milliseconds 300
    }
    [IO.Directory]::CreateDirectory($backup) | Out-Null
    foreach ($file in $data.Files) {
        $destination = Join-Path $target $file.Path
        $source = Join-Path $stage $file.Path
        $saved = Join-Path $backup $file.Path
        Assert-NoLink $destination
        Assert-NoLink $source
        Assert-NoLink $saved
        if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $file.Sha256) { throw 'Staged update changed.' }
        $existed = Test-Path -LiteralPath $destination
        if ($existed) {
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($saved)) | Out-Null
            [IO.File]::Copy($destination, $saved, $false)
        }
        $applied.Add([pscustomobject]@{ Destination = $destination; Saved = $saved; Existed = $existed })
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
        [IO.File]::Copy($source, $destination, $true)
    }
    'Update installed successfully. Previous files are retained in backup.' | Set-Content -LiteralPath $log -Encoding UTF8
    Start-Process -FilePath (Join-Path $target 'ETBTool.exe') -WorkingDirectory $target
} catch {
    $failure = $_.Exception.Message
    for ($i = $applied.Count - 1; $i -ge 0; $i--) {
        $item = $applied[$i]
        try {
            Assert-NoLink $item.Destination
            if ($item.Existed) { [IO.File]::Copy($item.Saved, $item.Destination, $true) }
            elseif (Test-Path -LiteralPath $item.Destination) { [IO.File]::Delete($item.Destination) }
        } catch { $failure += "`r`nRollback failed: " + $_.Exception.Message }
    }
    $message = "更新安装失败，已尝试恢复原文件。请重新打开工具后重试。`r`nUpdate failed; rollback attempted. Restart ETBTool to retry.`r`n" + $failure + "`r`nBackup/log: " + $root
    $message | Set-Content -LiteralPath $log -Encoding UTF8
    try { Add-Type -AssemblyName PresentationFramework; [System.Windows.MessageBox]::Show($message, 'ETBTool 更新失败') | Out-Null } catch {}
    exit 1
}
""";

        private static string FormatBytes(long b) => b switch
        {
            < 1024 => $"{b} B", < 1048576 => $"{b / 1024.0:F1} KB",
            < 1073741824L => $"{b / 1048576.0:F1} MB", _ => $"{b / 1073741824.0:F2} GB"
        };

        public static void OpenUpdatePage(string? dl = null, string? mi = null)
        {
            ExternalLauncher.OpenWebsite(GamePaths.UpdateDownloadUrl);
        }
    }
}
