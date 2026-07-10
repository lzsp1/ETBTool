using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ETBTool.Models;
using ETBTool.Utils;
using ETBTool.Windows;

namespace ETBTool.Views
{
    public class CrashItem
    {
        public string Name { get; set; } = "";
        public string Size { get; set; } = "";
        public string Modified { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public partial class CrashLogPage : Page
    {
        private CrashItem[] _items = Array.Empty<CrashItem>();

        public CrashLogPage()
        {
            InitializeComponent();
            LogPathBox.Text = GamePaths.CrashLogPath;
            LoadFiles();
        }

        private void LoadFiles()
        {
            FileListBox.Items.Clear();
            try
            {
                if (!Directory.Exists(GamePaths.CrashLogPath))
                { _items = Array.Empty<CrashItem>(); return; }

                _items = Directory.GetFiles(GamePaths.CrashLogPath)
                    .Select(f =>
                    {
                        var fi = new FileInfo(f);
                        return new CrashItem
                        {
                            Name = fi.Name,
                            Size = $"{fi.Length / 1024.0:F1} KB",
                            Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                            FilePath = f
                        };
                    })
                    .OrderByDescending(x => x.Modified)
                    .ToArray();

                foreach (var item in _items) FileListBox.Items.Add(item);
                Logger.Log($"已加载 {_items.Length} 个日志");
            }
            catch (Exception ex) { Logger.Log($"读取失败: {ex.Message}"); }
        }

        private CrashItem[] GetSelectedItems()
        {
            var result = new List<CrashItem>();
            foreach (var obj in FileListBox.SelectedItems)
                if (obj is CrashItem ci) result.Add(ci);
            return result.ToArray();
        }

        private void OpenFolder_Click(object s, RoutedEventArgs e)
        {
            try
            {
                var path = GamePaths.CrashLogPath;
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void CopyPath_Click(object s, RoutedEventArgs e)
        {
            bool ok = Logger.SafeSetClipboard(GamePaths.CrashLogPath);
            Toast.Show(ok ? "已复制" : "复制失败", ok ? ToastType.Success : ToastType.Error);
        }

        private void Refresh_Click(object s, RoutedEventArgs e)
        { LoadFiles(); Toast.Show("已刷新", ToastType.Info); }

        private void Open_Click(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedItems();
            if (sel.Length == 0) return;
            try
            {
                foreach (var item in sel)
                    Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void Delete_Click(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedItems();
            if (sel.Length == 0) return;
            if (ThemedDialog.Show(Window.GetWindow(this),
                $"确定删除 {sel.Length} 个日志？", "确认",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                foreach (var item in sel) File.Delete(item.FilePath);
                LoadFiles();
                Toast.Show($"已删除 {sel.Length} 个", ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void ClearAll_Click(object s, RoutedEventArgs e)
        {
            if (ThemedDialog.Show(Window.GetWindow(this),
                "确定清空全部崩溃日志？", "确认",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                foreach (var f in Directory.GetFiles(GamePaths.CrashLogPath)) File.Delete(f);
                LoadFiles();
                Toast.Show("已清空", ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}