using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using ETBTool.Models;

namespace ETBTool.Utils
{
    public static class ExternalLauncher
    {
        internal sealed record LaunchPlan(ProcessStartInfo StartInfo, string? Warning = null);

        public static bool OpenWebsite(string url) => TryLaunch("打开网页", () => PlanWebsite(url, ReadRegistry, File.Exists));
        public static bool OpenFolder(string path) => TryLaunch("打开文件夹", () => PlanFolder(path, Directory.Exists));
        public static bool OpenFile(string path) => TryLaunch("打开文件", () => PlanFile(path, File.Exists));
        public static bool SelectFile(string path) => TryLaunch("定位文件", () => PlanFolder(path, File.Exists, true));

        public static bool OpenAppConfigFolder() => TryLaunch("打开配置文件夹", () =>
        {
            if (!Directory.Exists(GamePaths.AppConfigPath)) Directory.CreateDirectory(GamePaths.AppConfigPath);
            return PlanFolder(GamePaths.AppConfigPath, Directory.Exists);
        });

        private static bool TryLaunch(string action, Func<LaunchPlan> makePlan)
        {
            try
            {
                var plan = makePlan();
                if (plan.Warning != null) Logger.Log("外部打开提示：" + plan.Warning);
                Logger.Log($"{action}：交给 Windows Shell；目标={plan.StartInfo.FileName}；参数={plan.StartInfo.Arguments}{string.Join(" | ", plan.StartInfo.ArgumentList)}");
                using var process = Process.Start(plan.StartInfo);
                Logger.Log($"{action}：系统打开请求已提交。");
                return true;
            }
            catch (Exception ex)
            {
                string reason = ex is System.ComponentModel.Win32Exception native ? native.NativeErrorCode switch
                {
                    5 => "Windows 拒绝访问（错误 5）" + (RuntimeEnvironment.IsLowIntegrity() ? "。程序正在受限环境中运行，请把 ZIP 解压到桌面等普通文件夹后重试。" : "。"),
                    2 or 3 => "系统找不到目标文件或目录。",
                    1155 => "系统尚未设置默认打开程序。",
                    1223 => "已取消系统打开请求。",
                    _ => Logger.NormalizeForDisplay(ex.Message)
                } : Logger.NormalizeForDisplay(ex.Message);
                var message = $"{action}失败：{reason}";
                Logger.Log(message);
                Toast.Show(message, ToastType.Error, 7000);
                return false;
            }
        }

        internal static LaunchPlan PlanWebsite(string url, Func<string, string, string?, string?> registry, Func<string, bool> fileExists)
        {
            if (string.IsNullOrWhiteSpace(url) || url.Any(char.IsControl) ||
                !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
                throw new ArgumentException("网址必须是有效的 HTTP 或 HTTPS 链接，且不能包含登录凭据或控制字符。");

            var progId = registry("用户", @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\" + uri.Scheme + @"\UserChoice", "ProgId");
            string? executable = null;
            if (!string.IsNullOrWhiteSpace(progId) && progId.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_'))
                executable = ParseExecutable(registry("类别", progId + @"\shell\open\command", null));
            var flag = (executable == null ? "" : Path.GetFileName(executable).ToLowerInvariant()) switch
            {
                "msedge.exe" or "chrome.exe" => "--new-window",
                "firefox.exe" => "-new-window",
                _ => null
            };
            if (flag != null && executable != null && fileExists(executable))
            {
                var start = new ProcessStartInfo(executable) { UseShellExecute = true, Verb = "open" };
                // Keep only the supported window flag, never arbitrary registry command arguments.
                start.ArgumentList.Add(flag);
                start.ArgumentList.Add(uri.AbsoluteUri);
                return new LaunchPlan(start);
            }
            return new LaunchPlan(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true, Verb = "open" },
                "默认浏览器未提供可识别的新窗口接口，使用 Windows 默认网址关联。");
        }

        internal static LaunchPlan PlanFolder(string path, Func<string, bool> exists, bool selectFile = false)
        {
            path = ValidatePath(path);
            if (!exists(path)) throw new IOException(selectFile ? "文件不存在或无法访问。" : "文件夹不存在或无法访问，请检查路径。");
            if (!selectFile) return new LaunchPlan(new ProcessStartInfo(path) { UseShellExecute = true, Verb = "open" });
            return new LaunchPlan(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"))
            {
                UseShellExecute = true,
                Verb = "open",
                Arguments = "/select," + QuoteArgument(path)
            });
        }

        internal static LaunchPlan PlanFile(string path, Func<string, bool> exists)
        {
            path = ValidatePath(path);
            if (!exists(path)) throw new IOException("文件不存在或无法访问。");
            return new LaunchPlan(new ProcessStartInfo(path) { UseShellExecute = true, Verb = "open" });
        }

        internal static string? ParseExecutable(string? command)
        {
            if (string.IsNullOrWhiteSpace(command) || command.Any(char.IsControl)) return null;
            command = command.Trim();
            string executable;
            if (command[0] == '"')
            {
                int end = command.IndexOf('"', 1);
                if (end < 0 || (end + 1 < command.Length && !char.IsWhiteSpace(command[end + 1]))) return null;
                executable = command.Substring(1, end - 1);
            }
            else
            {
                int end = command.IndexOfAny(new[] { ' ', '\t' });
                executable = end < 0 ? command : command.Substring(0, end);
            }
            executable = Environment.ExpandEnvironmentVariables(executable);
            if (!Path.IsPathFullyQualified(executable) || executable.Contains('"') ||
                !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || executable.StartsWith(@"\\", StringComparison.Ordinal)) return null;
            return executable;
        }

        private static string ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Any(char.IsControl) || path.Contains('"') || !Path.IsPathFullyQualified(path))
                throw new ArgumentException("路径必须是有效的完整文件或文件夹路径。");
            return Path.GetFullPath(path);
        }

        private static string? ReadRegistry(string root, string path, string? name)
        {
            try
            {
                using var key = (root == "用户" ? Registry.CurrentUser : Registry.ClassesRoot).OpenSubKey(path);
                return key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
            }
            catch (UnauthorizedAccessException) { return null; }
            catch (System.Security.SecurityException) { return null; }
            catch (IOException) { return null; }
        }

        internal static string QuoteArgument(string value)
        {
            var result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char c in value)
            {
                if (c == '\\') { slashes++; continue; }
                result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes);
                result.Append(c);
                slashes = 0;
            }
            return result.Append('\\', slashes * 2).Append('"').ToString();
        }
    }
}
