using System;
using System.IO;
using System.Windows;
using ETBTool.Models;

namespace ETBTool.Utils
{
    public static class Logger
    {
        private static readonly object _lock = new();
        public static event Action<string>? OnLogAdded;

        public static void Log(string message)
        {
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
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