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
    public class SaveItem
    {
        public string Difficulty { get; set; } = "";
        public string DiffColor { get; set; } = "#888888";
        public string SaveName { get; set; } = "";
        public string Size { get; set; } = "";
        public string Modified { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public partial class SaveManagerPage : Page
    {
        private SaveItem[] _items = Array.Empty<SaveItem>();
        private static readonly string[] SaveExts = { ".sav", ".sav1", ".sav2", ".sav3" };

        private static readonly (string tag, string cn, string color)[] Diffs =
        {
            ("Easy",      "简单", "#2ECC71"),
            ("Normal",    "普通", "#3498DB"),
            ("Hard",      "困难", "#E67E22"),
            ("Nightmare", "噩梦", "#E74C3C"),
            ("Practice",  "练习", "#9B59B6"),
            ("Tutorial",  "教程", "#1ABC9C"),
        };

        public SaveManagerPage()
        {
            InitializeComponent();
            SavePathBox.Text = GamePaths.SavePath;
            LoadFiles();
        }

        private static (string name, string diff, string color) ParseSaveFile(string fileName)
        {
            string raw = Path.GetFileNameWithoutExtension(fileName);
            if (!raw.StartsWith("MULTIPLAYER_", StringComparison.OrdinalIgnoreCase))
                return ("", "", "");
            foreach (var (tag, cn, color) in Diffs)
            {
                var suffix = "_" + tag;
                if (raw.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var saveName = raw.Substring("MULTIPLAYER_".Length,
                        raw.Length - "MULTIPLAYER_".Length - suffix.Length);
                    return (saveName, cn, color);
                }
            }
            var name = raw.Substring("MULTIPLAYER_".Length);
            return (name, "?", "#888888");
        }

        private void LoadFiles()
        {
            FileListBox.Items.Clear();
            try
            {
                if (!Directory.Exists(GamePaths.SavePath))
                { _items = Array.Empty<SaveItem>(); return; }

                _items = Directory.GetFiles(GamePaths.SavePath)
                    .Where(f => SaveExts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Select(f =>
                    {
                        var fi = new FileInfo(f);
                        var (name, diff, color) = ParseSaveFile(fi.Name);
                        if (string.IsNullOrEmpty(name)) return null;
                        return new SaveItem
                        {
                            Difficulty = diff,
                            DiffColor = color,
                            SaveName = name,
                            Size = $"{fi.Length / 1024.0:F1} KB",
                            Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                            FilePath = f
                        };
                    })
                    .Where(x => x != null).Cast<SaveItem>()
                    .OrderByDescending(x => x.Modified)
                    .ToArray();

                foreach (var item in _items) FileListBox.Items.Add(item);
                Logger.Log($"已加载 {_items.Length} 个存档");
            }
            catch (Exception ex) { Logger.Log($"读取失败: {ex.Message}"); }
        }

        // ★ 获取所有选中项
        private SaveItem[] GetSelectedItems()
        {
            var result = new List<SaveItem>();
            foreach (var obj in FileListBox.SelectedItems)
                if (obj is SaveItem si) result.Add(si);
            return result.ToArray();
        }

        private void OpenFolder(object s, RoutedEventArgs e)
        {
            try
            {
                var path = GamePaths.SavePath;
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void CopyPath(object s, RoutedEventArgs e)
        {
            bool ok = Logger.SafeSetClipboard(GamePaths.SavePath);
            Toast.Show(ok ? "已复制" : "复制失败", ok ? ToastType.Success : ToastType.Error);
        }

        private void Refresh(object s, RoutedEventArgs e) { LoadFiles(); Toast.Show("已刷新", ToastType.Info); }

        private void Edit(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedItems();
            if (sel.Length == 0) return;
            try
            {
                Process.Start(new ProcessStartInfo(sel[0].FilePath) { UseShellExecute = true });
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        // ★ 多选删除
        private void Delete(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedItems();
            if (sel.Length == 0) return;
            if (ThemedDialog.Show(Window.GetWindow(this),
                $"确定删除 {sel.Length} 个存档？", "确认",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                foreach (var item in sel) File.Delete(item.FilePath);
                LoadFiles();
                Toast.Show($"已删除 {sel.Length} 个", ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void ClearAll(object s, RoutedEventArgs e)
        {
            if (ThemedDialog.Show(Window.GetWindow(this),
                "确定清理全部存档？不可撤销！", "确认",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                foreach (var f in Directory.GetFiles(GamePaths.SavePath)
                    .Where(f => SaveExts.Contains(Path.GetExtension(f).ToLowerInvariant())))
                    File.Delete(f);
                LoadFiles();
                Toast.Show("清理完成", ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}