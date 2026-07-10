using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ETBTool.Models;
using ETBTool.Utils;
using ETBTool.Windows;
using Microsoft.Win32;

namespace ETBTool.Views
{
    public class FileItem
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Size { get; set; } = "";
        public string Modified { get; set; } = "";
    }

    public partial class UE4ManagerPage : Page
    {
        private string _rootDir = "";
        private string _currentDir = "";
        private string[] _dirs = Array.Empty<string>();
        private string[] _files = Array.Empty<string>();

        public UE4ManagerPage()
        {
            InitializeComponent();
            _rootDir = GamePaths.UE4Path;
            PathBox.Text = _rootDir;
            NavigateTo(_rootDir);
        }

        private void NavigateTo(string path)
        {
            _currentDir = path;
            CurrentDirText.Text = path;
            LoadItems();
        }

        private void LoadItems()
        {
            ItemListBox.Items.Clear();
            try
            {
                if (!Directory.Exists(_currentDir))
                { _dirs = Array.Empty<string>(); _files = Array.Empty<string>(); return; }

                _dirs = Directory.GetDirectories(_currentDir).OrderBy(d => d).ToArray();
                _files = Directory.GetFiles(_currentDir).OrderBy(f => f).ToArray();

                bool showUp = _currentDir != _rootDir && Directory.GetParent(_currentDir) != null;
                if (showUp)
                    ItemListBox.Items.Add(new FileItem { Name = "..  (上级目录)", Type = "文件夹", Size = "-", Modified = "-" });

                foreach (var d in _dirs)
                {
                    var i = new DirectoryInfo(d);
                    ItemListBox.Items.Add(new FileItem
                    {
                        Name = i.Name,
                        Type = "文件夹",
                        Size = "-",
                        Modified = i.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    });
                }
                foreach (var f in _files)
                {
                    var i = new FileInfo(f);
                    ItemListBox.Items.Add(new FileItem
                    {
                        Name = i.Name,
                        Type = i.Extension.TrimStart('.').ToUpperInvariant(),
                        Size = $"{i.Length / 1024.0:F1} KB",
                        Modified = i.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    });
                }
            }
            catch (Exception ex) { Logger.Log($"读取失败: {ex.Message}"); }
        }

        private int GetOffset() =>
            (_currentDir != _rootDir && Directory.GetParent(_currentDir) != null) ? 1 : 0;

        private void Item_DblClick(object s, MouseButtonEventArgs e)
        {
            var idx = ItemListBox.SelectedIndex;
            if (idx < 0) return;
            var off = GetOffset();
            if (off == 1 && idx == 0) { GoUp(s, new RoutedEventArgs()); return; }
            var realIdx = idx - off;
            if (realIdx >= 0 && realIdx < _dirs.Length)
                NavigateTo(_dirs[realIdx]);
            else
            {
                var fi = realIdx - _dirs.Length;
                if (fi >= 0 && fi < _files.Length)
                    try { Process.Start("explorer.exe", $"/select,\"{_files[fi]}\""); } catch { }
            }
        }

        private void GoUp(object s, RoutedEventArgs e)
        {
            var parent = Directory.GetParent(_currentDir);
            if (parent != null && _currentDir != _rootDir) NavigateTo(parent.FullName);
        }

        // ★ 弹窗提示 → 复制路径 → 打开"此电脑"
        private void OpenRoot(object s, RoutedEventArgs e)
        {
            var answer = ThemedDialog.Show(
                Window.GetWindow(this),
                "路径跳转 Bug 正在全力修复中，请您手动粘贴路径。\n\n点击\"确定\"将自动复制路径并打开资源管理器，\n点击\"取消\"不做任何操作。",
                "提示",
                MessageBoxButton.OKCancel);

            if (answer != MessageBoxResult.OK) return;

            try
            {
                bool ok = Logger.SafeSetClipboard(_rootDir);
                if (ok)
                    Toast.Show("路径已复制到剪贴板，请在资源管理器地址栏中粘贴", ToastType.Success);
                else
                    Toast.Show("复制失败，请手动记录路径", ToastType.Warning);

                Process.Start("explorer.exe",
                    Environment.GetFolderPath(Environment.SpecialFolder.MyComputer));

                Logger.Log($"UE4 打开路径: {_rootDir}（已复制到剪贴板）");
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void CopyRoot(object s, RoutedEventArgs e)
        {
            bool ok = Logger.SafeSetClipboard(_rootDir);
            Toast.Show(ok ? "已复制" : "复制失败", ok ? ToastType.Success : ToastType.Error);
        }

        private void Refresh(object s, RoutedEventArgs e)
        {
            LoadItems();
            Toast.Show("已刷新", ToastType.Info);
        }

        private void Import(object s, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Title = "选择文件", Multiselect = true, Filter = "所有文件|*.*" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                foreach (var f in dlg.FileNames)
                    File.Copy(f, Path.Combine(_currentDir, Path.GetFileName(f)), true);
                LoadItems();
                Toast.Show($"已导入 {dlg.FileNames.Length} 个文件", ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        // ★ 多选删除
        private void DelSel(object s, RoutedEventArgs e)
        {
            var selected = new List<(string path, bool isDir)>();
            foreach (var obj in ItemListBox.SelectedItems)
            {
                int idx = ItemListBox.Items.IndexOf(obj) - GetOffset();
                if (idx >= 0 && idx < _dirs.Length)
                    selected.Add((_dirs[idx], true));
                else if (idx >= _dirs.Length && idx - _dirs.Length < _files.Length)
                    selected.Add((_files[idx - _dirs.Length], false));
            }
            if (selected.Count == 0) return;
            if (ThemedDialog.Show(Window.GetWindow(this),
                $"确定删除 {selected.Count} 个项目？", "确认",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                foreach (var (path, isDir) in selected)
                    if (isDir) Directory.Delete(path, true); else File.Delete(path);
                LoadItems();
                Toast.Show($"已删除 {selected.Count} 个", ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        // ★ 清空后跳转到 Steam 库的逃离后室游戏界面
        private void DelAll(object s, RoutedEventArgs e)
        {
            if (ThemedDialog.Show(Window.GetWindow(this),
                "一键全部清空游戏文件？\n\n清空后将自动跳转到 Steam 库的逃离后室游戏界面，\n您可以在此重新下载/验证游戏文件。\n\n不可撤销！",
                "确认", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                foreach (var d in Directory.GetDirectories(_rootDir)) Directory.Delete(d, true);
                foreach (var f in Directory.GetFiles(_rootDir)) File.Delete(f);

                // ★ 跳转到 Steam 库中的逃离后室游戏界面
                Process.Start(new ProcessStartInfo(GamePaths.SteamGameUri) { UseShellExecute = true });

                NavigateTo(_rootDir);
                Toast.Show("已清空，正在跳转 Steam 游戏页面", ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}