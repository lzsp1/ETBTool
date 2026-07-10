using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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
    public class ModItem
    {
        public string Status { get; set; } = "";
        public string StatusColor { get; set; } = "#888888";
        public string DisplayName { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Size { get; set; } = "";
        public string Modified { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public partial class ModManagerPage : Page
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(15),
            DefaultRequestHeaders = { { "User-Agent", "ETBTool/0.0.02" } }
        };

        private static readonly string FilteredFile = "EscapeTheBackrooms-WindowsNoEditor.pak";

        private string _rootDir = "";
        private string _currentDir = "";
        private string[] _dirs = Array.Empty<string>();
        private ModItem[] _modItems = Array.Empty<ModItem>();
        private Dictionary<string, string> _trans = new(StringComparer.OrdinalIgnoreCase);

        public ModManagerPage()
        {
            InitializeComponent();
            _rootDir = GamePaths.ModPath;
            ModPathBox.Text = _rootDir;
            NavigateTo(_rootDir);
            _ = LoadTransAsync();
        }

        private void NavigateTo(string path)
        {
            _currentDir = path;
            CurrentDirText.Text = path;
            LoadItems();
        }

        private static bool IsModFile(string fileName)
        {
            var name = fileName;
            if (name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
                name = Path.GetFileNameWithoutExtension(name);
            var ext = Path.GetExtension(name).ToLowerInvariant();
            var baseName = Path.GetFileName(name);
            return (ext == ".pak" || ext == ".utoc") &&
                !baseName.Equals(FilteredFile, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetBaseName(string fileName)
        {
            if (fileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
                return Path.GetFileNameWithoutExtension(fileName);
            return fileName;
        }

        private void LoadItems()
        {
            ModListBox.Items.Clear();
            try
            {
                if (!Directory.Exists(_currentDir))
                { _dirs = Array.Empty<string>(); _modItems = Array.Empty<ModItem>(); return; }

                _dirs = Directory.GetDirectories(_currentDir).OrderBy(d => d).ToArray();

                var rawFiles = Directory.GetFiles(_currentDir)
                    .Where(f => IsModFile(Path.GetFileName(f)))
                    .OrderBy(f => f).ToArray();

                bool showUp = _currentDir != _rootDir && Directory.GetParent(_currentDir) != null;
                if (showUp)
                    ModListBox.Items.Add(new ModItem
                    {
                        Status = "目录",
                        StatusColor = "#3498DB",
                        DisplayName = "..  (上级目录)",
                        FileName = "-",
                        Size = "-",
                        Modified = "-"
                    });

                foreach (var d in _dirs)
                {
                    var i = new DirectoryInfo(d);
                    ModListBox.Items.Add(new ModItem
                    {
                        Status = "目录",
                        StatusColor = "#3498DB",
                        DisplayName = i.Name,
                        FileName = "-",
                        Size = "-",
                        Modified = i.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    });
                }

                _modItems = rawFiles.Select(f =>
                {
                    var fi = new FileInfo(f);
                    var rawName = fi.Name;
                    bool disabled = rawName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                    var baseName = GetBaseName(rawName);
                    var cn = _trans.TryGetValue(baseName, out var t) ? t : baseName;
                    return new ModItem
                    {
                        Status = disabled ? "OFF" : "ON",
                        StatusColor = disabled ? "#E74C3C" : "#2ECC71",
                        DisplayName = cn,
                        FileName = baseName,
                        Size = $"{fi.Length / 1024.0:F1} KB",
                        Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        FilePath = f
                    };
                }).ToArray();

                foreach (var item in _modItems) ModListBox.Items.Add(item);
            }
            catch (Exception ex) { Logger.Log($"读取失败: {ex.Message}"); }
        }

        private int GetDirOffset() =>
            (_currentDir != _rootDir && Directory.GetParent(_currentDir) != null) ? 1 : 0;

        private ModItem[] GetSelectedMods()
        {
            var result = new List<ModItem>();
            foreach (var obj in ModListBox.SelectedItems)
                if (obj is ModItem mi && !string.IsNullOrEmpty(mi.FilePath))
                    result.Add(mi);
            return result.ToArray();
        }

        private async Task LoadTransAsync()
        {
            Dispatcher.Invoke(() => LoadingHint.Visibility = Visibility.Visible);
            try
            {
                var json = await _http.GetStringAsync(GamePaths.ModNamesUrl);
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var root = JsonDocument.Parse(json).RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                    foreach (var p in root.EnumerateObject())
                        dict[p.Name] = p.Value.GetString() ?? "";
                else if (root.ValueKind == JsonValueKind.Array)
                    foreach (var item in root.EnumerateArray())
                        if (item.TryGetProperty("filename", out var fn) &&
                            item.TryGetProperty("name", out var cn))
                            dict[fn.GetString() ?? ""] = cn.GetString() ?? "";
                _trans = dict;
                Logger.Log($"已加载 {_trans.Count} 条翻译");
            }
            catch (Exception ex) { Logger.Log($"翻译加载失败: {ex.Message}"); }
            Dispatcher.Invoke(() => { LoadingHint.Visibility = Visibility.Collapsed; LoadItems(); });
        }

        private void ModDblClick(object s, MouseButtonEventArgs e)
        {
            var idx = ModListBox.SelectedIndex;
            if (idx < 0) return;
            var off = GetDirOffset();
            if (off == 1 && idx == 0) { GoUp(s, new RoutedEventArgs()); return; }
            var realIdx = idx - off;
            if (realIdx >= 0 && realIdx < _dirs.Length)
                NavigateTo(_dirs[realIdx]);
            else
            {
                if (ModListBox.SelectedItem is ModItem mi && !string.IsNullOrEmpty(mi.FilePath))
                    try { Process.Start("explorer.exe", $"/select,\"{mi.FilePath}\""); } catch { }
            }
        }

        private void GoUp(object s, RoutedEventArgs e)
        {
            var p = Directory.GetParent(_currentDir);
            if (p != null && _currentDir != _rootDir) NavigateTo(p.FullName);
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

                Logger.Log($"Mod 打开路径: {_rootDir}（已复制到剪贴板）");
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void CopyRoot(object s, RoutedEventArgs e)
        {
            bool ok = Logger.SafeSetClipboard(_rootDir);
            Toast.Show(ok ? "已复制" : "复制失败", ok ? ToastType.Success : ToastType.Error);
        }

        private void Refresh(object s, RoutedEventArgs e)
        { _ = LoadTransAsync(); Toast.Show("正在刷新...", ToastType.Info); }

        private void Import(object s, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Title = "选择 Mod", Multiselect = true, Filter = "Mod|*.pak;*.utoc|所有文件|*.*" };
            if (dlg.ShowDialog() != true) return;
            try { foreach (var f in dlg.FileNames) File.Copy(f, Path.Combine(_currentDir, Path.GetFileName(f)), true); LoadItems(); Toast.Show($"已导入 {dlg.FileNames.Length} 个", ToastType.Success); }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void Pack(object s, RoutedEventArgs e)
        {
            var selected = GetSelectedMods();
            if (selected.Length == 0)
            { ThemedDialog.Show(Window.GetWindow(this), "请先选择 Mod 文件（支持 Ctrl+点击多选）"); return; }

            var dlg = new SaveFileDialog { Title = "保存 Mod 包", Filter = "ZIP|*.zip", FileName = "mod_pack.zip" };
            if (dlg.ShowDialog() != true) return;

            try
            {
                using var zip = ZipFile.Open(dlg.FileName, ZipArchiveMode.Create);
                foreach (var mod in selected)
                    zip.CreateEntryFromFile(mod.FilePath, Path.GetFileName(mod.FilePath));

                var entry = zip.CreateEntry("由逃离后室游戏工具打包生成.txt");
                using var writer = new StreamWriter(entry.Open(), System.Text.Encoding.UTF8);
                writer.WriteLine("=== ETBTool Mod Pack ===");
                writer.WriteLine($"打包时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"文件数量: {selected.Length}");
                writer.WriteLine();
                foreach (var mod in selected)
                {
                    writer.WriteLine($"文件名: {Path.GetFileName(mod.FilePath)}");
                    writer.WriteLine($"中文名: {mod.DisplayName}");
                    writer.WriteLine($"大小: {mod.Size}");
                    writer.WriteLine();
                }
                writer.WriteLine("逃离后室游戏工具遵循MIT开源协议，二创、下载工具请访问https://github.com/lzsp1/ETBTool");

                Toast.Show($"已打包 {selected.Length} 个文件", ToastType.Success);
            }
            catch (Exception ex) { Toast.Show($"打包失败: {ex.Message}", ToastType.Error); }
        }

        private void Del(object s, RoutedEventArgs e)
        {
            var selected = GetSelectedMods();
            if (selected.Length == 0) return;
            if (ThemedDialog.Show(Window.GetWindow(this), $"确定删除 {selected.Length} 个文件？", "确认", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try { foreach (var mod in selected) File.Delete(mod.FilePath); LoadItems(); Toast.Show($"已删除 {selected.Length} 个", ToastType.Success); }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        // ★ .disabled 后缀逻辑
        private void Toggle(object s, RoutedEventArgs e)
        {
            var selected = GetSelectedMods();
            if (selected.Length == 0) return;
            int enabled = 0, disabled = 0;
            try
            {
                foreach (var mod in selected)
                {
                    var fi = new FileInfo(mod.FilePath);
                    var dir = fi.DirectoryName!;
                    var name = fi.Name;

                    if (name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
                    {
                        var original = Path.Combine(dir, Path.GetFileNameWithoutExtension(name));
                        File.Move(fi.FullName, original);
                        enabled++;
                    }
                    else
                    {
                        File.Move(fi.FullName, fi.FullName + ".disabled");
                        disabled++;
                    }
                }
                LoadItems();
                Toast.Show($"启用:{enabled} 禁用:{disabled}", ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void ModSel(object s, SelectionChangedEventArgs e)
        {
            var mods = GetSelectedMods();
            if (mods.Length > 1)
                ToggleBtn.Content = $"禁用/启用 ({mods.Length})";
            else if (mods.Length == 1)
                ToggleBtn.Content = mods[0].Status == "OFF" ? "启用选中" : "禁用选中";
        }

        private void SubmitNick(object s, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(GamePaths.NicknameSubmitUrl) { UseShellExecute = true }); }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}