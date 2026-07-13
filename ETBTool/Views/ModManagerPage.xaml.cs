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
    public class ModItem { public string Status { get; set; } = ""; public string StatusColor { get; set; } = "#888"; public string DisplayName { get; set; } = ""; public string FileName { get; set; } = ""; public string Size { get; set; } = ""; public string Modified { get; set; } = ""; public string FilePath { get; set; } = ""; }

    public partial class ModManagerPage : Page
    {
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15), DefaultRequestHeaders = { { "User-Agent", "ETBTool/0.0.02" } } };
        private static readonly string FilteredFile = "EscapeTheBackrooms-WindowsNoEditor.pak";
        private string _rootDir = "", _currentDir = "";
        private string[] _dirs = Array.Empty<string>();
        private ModItem[] _modItems = Array.Empty<ModItem>();
        private Dictionary<string, string> _trans = new(StringComparer.OrdinalIgnoreCase);

        public ModManagerPage()
        {
            InitializeComponent(); LanguageManager.LanguageChanged += RefreshText; _rootDir = GamePaths.ModPath; ModPathBox.Text = _rootDir; NavigateTo(_rootDir);
            if (LanguageManager.IsChinese) _ = LoadTransAsync(); else LoadItems(); RefreshText();
        }

        private void RefreshText()
        {
            PageTitle.Text = LanguageManager.ModTitle; PageSub.Text = LanguageManager.ModSub; LblPath.Text = LanguageManager.ModPath;
            LblList.Text = LanguageManager.ModList; LoadingHint.Text = LanguageManager.ModLoading;
            BtnOpen.Content = LanguageManager.BtnOpen; BtnCopy.Content = LanguageManager.BtnCopy;
            HStatus.Text = LanguageManager.ColStatus; HName.Text = LanguageManager.ColName; HFile.Text = LanguageManager.ColFile; HSize.Text = LanguageManager.ColSize; HMod.Text = LanguageManager.ColModified;
            BtnUp.Content = LanguageManager.BtnUpDir; BtnRefresh.Content = LanguageManager.BtnRefresh;
            BtnImport.Content = LanguageManager.BtnImportMod; BtnPack.Content = LanguageManager.BtnPack; BtnDel.Content = LanguageManager.BtnDelete;
            LblActions.Text = LanguageManager.ModActions; ToggleBtn.Content = LanguageManager.BtnToggle;
            NickDesc.Text = LanguageManager.ModNickDesc; BtnNick.Content = LanguageManager.BtnSubmitNick;
        }

        private void NavigateTo(string path) { _currentDir = path; CurrentDirText.Text = path; LoadItems(); }

        private static bool IsModFile(string fileName) { var name = fileName; if (name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)) name = Path.GetFileNameWithoutExtension(name); var ext = Path.GetExtension(name).ToLowerInvariant(); return (ext == ".pak" || ext == ".utoc") && !Path.GetFileName(name).Equals(FilteredFile, StringComparison.OrdinalIgnoreCase); }
        private static string GetBaseName(string fileName) { if (fileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)) return Path.GetFileNameWithoutExtension(fileName); return fileName; }

        private void LoadItems()
        {
            ModListBox.Items.Clear(); try
            {
                if (!Directory.Exists(_currentDir)) { _dirs = Array.Empty<string>(); _modItems = Array.Empty<ModItem>(); return; }
                _dirs = Directory.GetDirectories(_currentDir).OrderBy(d => d).ToArray();
                var rawFiles = Directory.GetFiles(_currentDir).Where(f => IsModFile(Path.GetFileName(f))).OrderBy(f => f).ToArray();
                bool showUp = _currentDir != _rootDir && Directory.GetParent(_currentDir) != null;
                if (showUp) ModListBox.Items.Add(new ModItem { Status = LanguageManager.ColFolder, StatusColor = "#3498DB", DisplayName = LanguageManager.MsgUpDir, FileName = "-", Size = "-", Modified = "-" });
                foreach (var d in _dirs) { var i = new DirectoryInfo(d); ModListBox.Items.Add(new ModItem { Status = LanguageManager.ColFolder, StatusColor = "#3498DB", DisplayName = i.Name, FileName = "-", Size = "-", Modified = i.LastWriteTime.ToString("yyyy-MM-dd HH:mm") }); }
                _modItems = rawFiles.Select(f => {
                    var fi = new FileInfo(f); bool disabled = fi.Name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase); var baseName = GetBaseName(fi.Name);
                    var dn = LanguageManager.IsChinese ? (_trans.TryGetValue(baseName, out var t) ? t : baseName) : baseName;
                    return new ModItem { Status = disabled ? "OFF" : "ON", StatusColor = disabled ? "#E74C3C" : "#2ECC71", DisplayName = dn, FileName = baseName, Size = $"{fi.Length / 1024.0:F1} KB", Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"), FilePath = f };
                }).ToArray();
                foreach (var item in _modItems) ModListBox.Items.Add(item);
            }
            catch (Exception ex) { Logger.Log(ex.Message); }
        }

        private int GetDirOffset() => (_currentDir != _rootDir && Directory.GetParent(_currentDir) != null) ? 1 : 0;
        private ModItem[] GetSelectedMods() { var r = new List<ModItem>(); foreach (var o in ModListBox.SelectedItems) if (o is ModItem mi && !string.IsNullOrEmpty(mi.FilePath)) r.Add(mi); return r.ToArray(); }

        private async Task LoadTransAsync()
        {
            Dispatcher.Invoke(() => LoadingHint.Visibility = Visibility.Visible); try
            {
                var json = await _http.GetStringAsync(GamePaths.ModNamesUrl); var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); var root = JsonDocument.Parse(json).RootElement;
                if (root.ValueKind == JsonValueKind.Object) foreach (var p in root.EnumerateObject()) dict[p.Name] = p.Value.GetString() ?? "";
                else if (root.ValueKind == JsonValueKind.Array) foreach (var item in root.EnumerateArray()) if (item.TryGetProperty("filename", out var fn) && item.TryGetProperty("name", out var cn)) dict[fn.GetString() ?? ""] = cn.GetString() ?? "";
                _trans = dict; Logger.Log($"已加载 {_trans.Count} 条翻译");
            }
            catch (Exception ex) { Logger.Log(ex.Message); }
            Dispatcher.Invoke(() => { LoadingHint.Visibility = Visibility.Collapsed; LoadItems(); });
        }

        private void ModDblClick(object s, MouseButtonEventArgs e)
        {
            var idx = ModListBox.SelectedIndex; if (idx < 0) return; var off = GetDirOffset(); if (off == 1 && idx == 0) { GoUp(s, new RoutedEventArgs()); return; }
            var realIdx = idx - off; if (realIdx >= 0 && realIdx < _dirs.Length) NavigateTo(_dirs[realIdx]);
            else { if (ModListBox.SelectedItem is ModItem mi && !string.IsNullOrEmpty(mi.FilePath)) try { Process.Start("explorer.exe", $"/select,\"{mi.FilePath}\""); } catch { } }
        }
        private void GoUp(object s, RoutedEventArgs e) { var p = Directory.GetParent(_currentDir); if (p != null && _currentDir != _rootDir) NavigateTo(p.FullName); }

        private void OpenRoot(object s, RoutedEventArgs e)
        {
            var answer = ThemedDialog.Show(Window.GetWindow(this), LanguageManager.TipOpenPath, LanguageManager.IsChinese ? "提示" : "Tip", MessageBoxButton.OKCancel);
            if (answer != MessageBoxResult.OK) return; try
            {
                bool ok = Logger.SafeSetClipboard(_rootDir); Toast.Show(ok ? LanguageManager.MsgCopied : LanguageManager.MsgCopyFail, ok ? ToastType.Success : ToastType.Warning);
                Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.MyComputer));
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
        private void CopyRoot(object s, RoutedEventArgs e) { bool ok = Logger.SafeSetClipboard(_rootDir); Toast.Show(ok ? LanguageManager.MsgCopied : LanguageManager.MsgCopyFail, ok ? ToastType.Success : ToastType.Error); }
        private void Refresh(object s, RoutedEventArgs e) { if (LanguageManager.IsChinese) _ = LoadTransAsync(); else LoadItems(); Toast.Show(LanguageManager.MsgRefreshed, ToastType.Info); }

        private void Import(object s, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Title = LanguageManager.BtnImportMod, Multiselect = true, Filter = "Mod|*.pak;*.utoc|All|*.*" }; if (dlg.ShowDialog() != true) return;
            try { foreach (var f in dlg.FileNames) File.Copy(f, Path.Combine(_currentDir, Path.GetFileName(f)), true); LoadItems(); Toast.Show(LanguageManager.MsgImportMod(dlg.FileNames.Length), ToastType.Success); } catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void Pack(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedMods(); if (sel.Length == 0) { ThemedDialog.Show(Window.GetWindow(this), LanguageManager.MsgSelMod); return; }
            var dlg = new SaveFileDialog { Title = LanguageManager.BtnPack, Filter = "ZIP|*.zip", FileName = "mod_pack.zip" }; if (dlg.ShowDialog() != true) return;
            try
            {
                using var zip = ZipFile.Open(dlg.FileName, ZipArchiveMode.Create); foreach (var m in sel) zip.CreateEntryFromFile(m.FilePath, Path.GetFileName(m.FilePath));
                var entry = zip.CreateEntry($"{LanguageManager.PackTxtName}.txt"); using var w = new StreamWriter(entry.Open(), System.Text.Encoding.UTF8);
                w.WriteLine("=== ETBTool Mod Pack ==="); w.WriteLine($"{(LanguageManager.IsChinese ? "打包时间" : "Time")}: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"); w.WriteLine($"{(LanguageManager.IsChinese ? "文件数量" : "Files")}: {sel.Length}"); w.WriteLine();
                foreach (var m in sel) { w.WriteLine($"{(LanguageManager.IsChinese ? "文件名" : "File")}: {Path.GetFileName(m.FilePath)}"); w.WriteLine($"{(LanguageManager.IsChinese ? "中文名" : "Name")}: {m.DisplayName}"); w.WriteLine($"{(LanguageManager.IsChinese ? "大小" : "Size")}: {m.Size}"); w.WriteLine(); }
                w.WriteLine(LanguageManager.PackFooter);
                Toast.Show(LanguageManager.MsgPackOK(sel.Length), ToastType.Success);
            }
            catch (Exception ex) { Toast.Show($"{LanguageManager.MsgPackFail}: {ex.Message}", ToastType.Error); }
        }

        private void Del(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedMods(); if (sel.Length == 0) return; if (ThemedDialog.Show(Window.GetWindow(this), LanguageManager.MsgConfirmDelMod(sel.Length), LanguageManager.BtnConfirm, MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try { foreach (var m in sel) File.Delete(m.FilePath); LoadItems(); Toast.Show(LanguageManager.MsgDelMod(sel.Length), ToastType.Success); } catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void Toggle(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedMods(); if (sel.Length == 0) return; int en = 0, dis = 0;
            try
            {
                foreach (var m in sel) { var fi = new FileInfo(m.FilePath); var dir = fi.DirectoryName!; if (fi.Name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)) { File.Move(fi.FullName, Path.Combine(dir, Path.GetFileNameWithoutExtension(fi.Name))); en++; } else { File.Move(fi.FullName, fi.FullName + ".disabled"); dis++; } }
                LoadItems(); Toast.Show(LanguageManager.MsgToggle(en, dis), ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void ModSel(object s, SelectionChangedEventArgs e) { var mods = GetSelectedMods(); if (mods.Length > 1) ToggleBtn.Content = $"{LanguageManager.BtnToggle} ({mods.Length})"; else if (mods.Length == 1) ToggleBtn.Content = mods[0].Status == "OFF" ? LanguageManager.BtnEnable : LanguageManager.BtnDisable; }
        private void SubmitNick(object s, RoutedEventArgs e) { try { Process.Start(new ProcessStartInfo(GamePaths.NicknameSubmitUrl) { UseShellExecute = true }); } catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); } }
    }
}