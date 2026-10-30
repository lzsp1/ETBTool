using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using ETBTool.Models;

namespace ETBTool.Utils
{
    /// <summary>
    /// 检测当前进程是否处在受限环境：低完整性级别会让写入游戏目录、创建存档备份
    /// 以及调用资源管理器全部失败，但表面看起来像程序没有反应。
    /// </summary>
    public static class RuntimeEnvironment
    {
        private const int TokenIntegrityLevel = 25;
        private const int MediumIntegrity = 0x2000;

        public static bool IsLowIntegrity()
        {
            try
            {
                if (!OpenProcessToken(GetCurrentProcess(), 0x0008, out var token)) return false;
                using (token)
                {
                    GetTokenInformation(token, TokenIntegrityLevel, IntPtr.Zero, 0, out int size);
                    if (size <= 0) return false;
                    IntPtr buffer = Marshal.AllocHGlobal(size);
                    try
                    {
                        if (!GetTokenInformation(token, TokenIntegrityLevel, buffer, size, out _)) return false;
                        IntPtr sid = Marshal.ReadIntPtr(buffer);
                        byte count = Marshal.ReadByte(GetSidSubAuthorityCount(sid));
                        if (count == 0) return false;
                        int level = Marshal.ReadInt32(GetSidSubAuthority(sid, (uint)(count - 1)));
                        return level < MediumIntegrity;
                    }
                    finally { Marshal.FreeHGlobal(buffer); }
                }
            }
            catch { return false; }
        }

        /// <summary>实际写一次再删除，避免只根据文件存在与否做错误判断。</summary>
        public static bool CanWriteAppConfig()
        {
            try
            {
                Directory.CreateDirectory(GamePaths.AppConfigPath);
                string probe = Path.Combine(GamePaths.AppConfigPath, ".write-test-" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(probe, "ETBTool");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        public static bool CanWriteLog()
        {
            try
            {
                string text = $"启动自检 {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}";
                File.AppendAllText(GamePaths.LogFilePath, text);
                return true;
            }
            catch { return false; }
        }

        public sealed record Report(bool LowIntegrity, bool Writable, bool LogWritable, string ProgramPath)
        {
            public bool Broken => LowIntegrity || !Writable || !LogWritable;
        }

        public static Report Inspect()
        {
            string path = "";
            try { path = Environment.ProcessPath ?? ""; } catch { }
            return new Report(IsLowIntegrity(), CanWriteAppConfig(), CanWriteLog(), path);
        }

        public static string Advice(Report report)
        {
            var lines = new System.Collections.Generic.List<string>();
            lines.Add("ETBTool 检测到当前运行环境受限，无法正常写入本地文件或打开文件夹。");
            if (report.LowIntegrity) lines.Add("· 程序以“低完整性”方式启动（通常是从下载工具的沙箱、压缩包预览窗口或受限工作目录直接运行）。");
            if (!report.Writable) lines.Add($"· 配置目录不可写：{GamePaths.AppConfigPath}");
            if (!report.LogWritable) lines.Add($"· 日志文件不可写：{GamePaths.LogFilePath}");
            lines.Add("");
            lines.Add("解决办法：把完整 ZIP 解压到桌面、D 盘等普通文件夹后，双击其中的 ETBTool.exe 运行。");
            lines.Add("不要从压缩软件里直接运行，也不要把程序放在受限/沙箱目录中。");
            if (!string.IsNullOrEmpty(report.ProgramPath)) lines.Add($"当前程序位置：{report.ProgramPath}");
            return string.Join(Environment.NewLine, lines);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr process, uint access, out SafeAccessTokenHandle token);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTokenInformation(SafeAccessTokenHandle token, int informationClass, IntPtr information, int length, out int returned);

        [DllImport("advapi32.dll")]
        private static extern IntPtr GetSidSubAuthority(IntPtr sid, uint index);

        [DllImport("advapi32.dll")]
        private static extern IntPtr GetSidSubAuthorityCount(IntPtr sid);
    }
}
