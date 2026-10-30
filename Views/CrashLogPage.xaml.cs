using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ETBTool.Models;
using ETBTool.Utils;
using ETBTool.Windows;
using Microsoft.Win32;

namespace ETBTool.Views
{
    public class CrashItem
    {
        public string Name { get; set; } = "";
        public string Tag { get; set; } = "";
        public string TagColor { get; set; } = "#888888";
        public string Size { get; set; } = "";
        public string Modified { get; set; } = "";
        public string FilePath { get; set; } = "";
        public bool IsDir { get; set; }
    }

    public partial class CrashLogPage : Page
    {
        private string _rootDir = "";
        private string _currentDir = "";
        private CrashItem[] _items = Array.Empty<CrashItem>();
        private enum ListState { Loading, Ready, Empty, Unavailable, Error }
        private ListState _state = ListState.Loading;
        private string _loadError = "";
        private int _loadVersion;

        public CrashLogPage()
        {
            InitializeComponent();
            var configuredPath = GamePaths.GetRawCustomCrashPath();
            _rootDir = string.IsNullOrWhiteSpace(configuredPath) ? GamePaths.CrashLogPath : configuredPath.Trim();
            _currentDir = _rootDir;
            LogPathBox.Text = _rootDir;
            Loaded += (_, _) =>
            {
                LanguageManager.LanguageChanged -= RefreshText;
                LanguageManager.LanguageChanged += RefreshText;
                NavigateTo(_currentDir);
            };
            Unloaded += (_, _) =>
            {
                LanguageManager.LanguageChanged -= RefreshText;
                _loadVersion++;
            };
            RefreshText();
        }

        private void RefreshText()
        {
            PageTitle.Text = LanguageManager.CrashTitle;
            PageSub.Text = LanguageManager.CrashSub;
            LblPath.Text = LanguageManager.CrashPath;
            LblList.Text = LanguageManager.CrashList;
            BtnOpen.Content = LanguageManager.BtnOpen;
            BtnCopy.Content = LanguageManager.BtnCopy;
            HTag.Text = LanguageManager.IsChinese ? "类型" : "Type";
            HName.Text = LanguageManager.ColName;
            HSize.Text = LanguageManager.ColSize;
            HMod.Text = LanguageManager.ColModified;
            BtnUp.Content = LanguageManager.BtnUpDir;
            BtnRefresh.Content = LanguageManager.BtnRefresh;
            BtnExport.Content = LanguageManager.IsChinese ? "导出" : "Export";
            BtnDel.Content = LanguageManager.BtnDelete;
            BtnClear.Content = LanguageManager.BtnClearLogs;
            RefreshState();
        }

        private void RefreshState()
        {
            bool chinese = LanguageManager.IsChinese;
            bool ready = _state == ListState.Ready;
            ItemListBox.Visibility = ready ? Visibility.Visible : Visibility.Collapsed;
            ListStatePanel.Visibility = ready ? Visibility.Collapsed : Visibility.Visible;
            ListLoadingBar.Visibility = _state == ListState.Loading ? Visibility.Visible : Visibility.Collapsed;
            ListStateIcon.Text = _state switch
            {
                ListState.Empty => "\uE8A5",
                ListState.Unavailable => "\uE8B7",
                ListState.Error => "\uE783",
                _ => "\uE895"
            };
            ListStateTitle.Text = _state switch
            {
                ListState.Loading => chinese ? "正在加载崩溃日志…" : "Loading crash logs...",
                ListState.Empty => chinese ? "暂无崩溃日志" : "No crash logs found",
                ListState.Unavailable => chinese ? "日志文件夹不可用" : "Crash log folder unavailable",
                ListState.Error => chinese ? "无法读取崩溃日志" : "Could not read crash logs",
                _ => ""
            };
            ListStateDetail.Text = _state switch
            {
                ListState.Empty => chinese ? "当前文件夹中没有日志文件或子文件夹。" : "This folder contains no log files or subfolders.",
                ListState.Unavailable => chinese ? "配置的文件夹不存在，或所在磁盘未连接。" : "The configured folder does not exist or its drive is disconnected.",
                ListState.Error => _loadError,
                _ => ""
            };
            BtnUp.IsEnabled = _currentDir != _rootDir && _state != ListState.Loading;
            BtnRefresh.IsEnabled = _state != ListState.Loading;
            BtnExport.IsEnabled = ready;
            BtnDel.IsEnabled = ready && ItemListBox.SelectedItems.Count > 0;
            BtnClear.IsEnabled = ready;
        }

        private void ItemSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BtnDel != null) BtnDel.IsEnabled = _state == ListState.Ready && ItemListBox.SelectedItems.Count > 0;
        }

        private void NavigateTo(string path)
        {
            _currentDir = path;
            CurrentDirText.Text = path;
            LoadItems();
        }

        private async void LoadItems()
        {
            var version = ++_loadVersion;
            var directory = _currentDir;
            bool chinese = LanguageManager.IsChinese;
            ItemListBox.ItemsSource = null;
            _items = Array.Empty<CrashItem>();
            _state = ListState.Loading;
            _loadError = "";
            RefreshState();
            try
            {
                var items = await Task.Run(() => ReadItems(directory, chinese));
                if (version != _loadVersion) return;
                _items = items;
                ItemListBox.ItemsSource = _items;
                _state = _items.Length == 0 ? ListState.Empty : ListState.Ready;
                Logger.Log(chinese ? $"已加载 {_items.Length} 项" : $"Loaded {_items.Length} items");
            }
            catch (DirectoryNotFoundException)
            {
                if (version != _loadVersion) return;
                _state = ListState.Unavailable;
            }
            catch (Exception ex)
            {
                if (version != _loadVersion) return;
                _state = ListState.Error;
                _loadError = ex.Message;
                Logger.Log($"Crash log loading failed: {ex.Message}");
            }
            if (version == _loadVersion) RefreshState();
        }

        private static CrashItem[] ReadItems(string directory, bool chinese)
        {
            var list = new List<CrashItem>();
            foreach (var d in Directory.GetDirectories(directory).OrderBy(d => d))
            {
                var info = new DirectoryInfo(d);
                list.Add(new CrashItem
                {
                    Name = info.Name,
                    Tag = chinese ? "文件夹" : "Folder",
                    TagColor = "#3498DB",
                    Size = "-",
                    Modified = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                    FilePath = d,
                    IsDir = true
                });
            }
            foreach (var f in Directory.GetFiles(directory).OrderBy(f => f))
            {
                var info = new FileInfo(f);
                var extension = info.Extension.ToLowerInvariant();
                bool mainLog = extension == ".runtime-xml";
                list.Add(new CrashItem
                {
                    Name = info.Name,
                    Tag = mainLog ? (chinese ? "主要日志" : "Main Log")
                        : (string.IsNullOrEmpty(extension) ? "-" : extension.TrimStart('.').ToUpperInvariant()),
                    TagColor = mainLog ? "#2ECC71" : "#888888",
                    Size = $"{info.Length / 1024.0:F1} KB",
                    Modified = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                    FilePath = f
                });
            }
            return list.ToArray();
        }

        private CrashItem? GetSelected()
        {
            var idx = ItemListBox.SelectedIndex;
            if (idx < 0 || idx >= _items.Length) return null;
            return _items[idx];
        }

        private CrashItem[] GetSelectedItems()
        {
            var r = new List<CrashItem>();
            foreach (var o in ItemListBox.SelectedItems)
            {
                var idx = ItemListBox.Items.IndexOf(o);
                if (idx >= 0 && idx < _items.Length) r.Add(_items[idx]);
            }
            return r.ToArray();
        }

        private void Item_DblClick(object s, MouseButtonEventArgs e)
        {
            var item = GetSelected();
            if (item == null) return;

            if (item.IsDir)
            {
                NavigateTo(item.FilePath);
                return;
            }

            // .runtime-xml 文件：打开查看器
            if (item.FilePath.EndsWith(".runtime-xml", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var viewer = new CrashLogViewerWindow(item.FilePath);
                    viewer.Owner = Window.GetWindow(this);
                    viewer.ShowDialog();
                }
                catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
                return;
            }

            // 其它文件：用系统默认程序打开
            ExternalLauncher.OpenFile(item.FilePath);
        }

        private void GoUp_Click(object s, RoutedEventArgs e)
        {
            if (_currentDir != _rootDir)
            {
                var p = Directory.GetParent(_currentDir);
                if (p != null) NavigateTo(p.FullName);
            }
        }

        private void OpenFolder_Click(object s, RoutedEventArgs e)
        {
            ExternalLauncher.OpenFolder(_currentDir);
        }

        private void CopyPath_Click(object s, RoutedEventArgs e)
        {
            bool ok = Logger.SafeSetClipboard(_rootDir);
            Toast.Show(ok ? LanguageManager.MsgCopied : LanguageManager.MsgCopyFail,
                ok ? ToastType.Success : ToastType.Error);
        }

        private void Refresh_Click(object s, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void Export_Click(object s, RoutedEventArgs e)
        {
            // 导出当前目录或选中目录下的所有 .runtime-xml
            var items = GetSelectedItems();
            var targetDirs = items.Where(i => i.IsDir && !i.Name.StartsWith("▲")).Select(i => i.FilePath).ToList();
            if (targetDirs.Count == 0)
                targetDirs.Add(_currentDir);

            var files = new List<string>();
            foreach (var dir in targetDirs)
            {
                try
                {
                    files.AddRange(Directory.GetFiles(dir, "*.runtime-xml", SearchOption.AllDirectories));
                }
                catch { }
            }

            if (files.Count == 0)
            {
                Toast.Show(LanguageManager.IsChinese ? "没有找到主要日志文件" : "No main log files found", ToastType.Warning);
                return;
            }

            var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = LanguageManager.IsChinese ? "选择导出目录" : "Select export folder"
            };
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            try
            {
                int count = 0;
                foreach (var f in files)
                {
                    var rel = Path.GetRelativePath(_currentDir, f);
                    var dest = Path.Combine(dlg.SelectedPath, rel);
                    var destDir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);
                    File.Copy(f, dest, true);
                    count++;
                }
                Toast.Show(LanguageManager.IsChinese ? $"已导出 {count} 个主要日志" : $"Exported {count} main logs", ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void Delete_Click(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedItems().Where(i => !(i.IsDir && i.Name.StartsWith("▲"))).ToArray();
            if (sel.Length == 0) return;
            if (ThemedDialog.Show(Window.GetWindow(this),
                LanguageManager.MsgConfirmDelLog(sel.Length),
                LanguageManager.BtnConfirm,
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                foreach (var item in sel)
                {
                    if (item.IsDir) Directory.Delete(item.FilePath, true);
                    else File.Delete(item.FilePath);
                }
                LoadItems();
                Toast.Show(LanguageManager.MsgSaveDeleted(sel.Length), ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void ClearAll_Click(object s, RoutedEventArgs e)
        {
            if (ThemedDialog.Show(Window.GetWindow(this),
                LanguageManager.MsgConfirmClearLogs,
                LanguageManager.BtnConfirm,
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                // 只清根目录下的所有子文件夹（保留根目录本身）
                foreach (var d in Directory.GetDirectories(_rootDir))
                    Directory.Delete(d, true);
                foreach (var f in Directory.GetFiles(_rootDir))
                    File.Delete(f);
                NavigateTo(_rootDir);
                Toast.Show(LanguageManager.MsgCrashLogCleared, ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}