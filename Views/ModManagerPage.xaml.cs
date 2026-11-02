using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ETBTool.Models;
using ETBTool.Services;
using ETBTool.Utils;
using ETBTool.Windows;
using Microsoft.Win32;

namespace ETBTool.Views
{
    public class ModEntry : INotifyPropertyChanged
    {
        public string DisplayName { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Size { get; set; } = "";
        public string Modified { get; set; } = "";
        public string Status { get; set; } = "";

        // WPF Brush 是 Freezable/DependencyObject，不能在 Task.Run 线程创建后直接绑定到 UI。
        // 使用已冻结的 Brush，可安全地跨线程读取。
        private static readonly Brush EnabledStatusBrush = CreateStatusBrush(40, 167, 69);
        private static readonly Brush DisabledStatusBrush = CreateStatusBrush(220, 53, 69);

        public Brush StatusColor { get; set; } = Brushes.Gray;

        private static Brush CreateStatusBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        public static Brush GetStatusBrush(bool disabled) =>
            disabled ? DisabledStatusBrush : EnabledStatusBrush;

        public string FullPath { get; set; } = "";
        public bool IsDirectory { get; set; }
        public bool IsEnabled { get; set; } = true;
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnChanged(string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public partial class ModManagerPage : Page
    {
        private readonly ObservableCollection<ModEntry> _entries = new();
        private readonly ModTranslationService _translation = new();
        private string _currentDir = "";
        private int _loadGeneration;
        private bool _operating;

        public ModManagerPage()
        {
            try
            {
                Logger.Log("ModManagerPage: ctor start");

                InitializeComponent();
                Logger.Log("ModManagerPage: InitializeComponent OK");

                ModListBox.ItemsSource = _entries;
                Logger.Log("ModManagerPage: ItemsSource OK");

                LanguageManager.LanguageChanged += RefreshText;
                _translation.TranslationsChanged += OnTranslationsChanged;
                Unloaded += (_, _) =>
                {
                    LanguageManager.LanguageChanged -= RefreshText;
                    _translation.TranslationsChanged -= OnTranslationsChanged;
                    _translation.Dispose();
                };

                RefreshText();
                Logger.Log("ModManagerPage: RefreshText OK");

                try
                {
                    _currentDir = GamePaths.ModPath ?? "";
                    Logger.Log($"ModManagerPage: ModPath = {_currentDir}");
                }
                catch (Exception ex)
                {
                    _currentDir = "";
                    Logger.Log($"ModManagerPage: GamePaths.ModPath failed: {ex.Message}");
                }

                if (ModPathBox != null) ModPathBox.Text = _currentDir;

                Loaded += async (_, _) =>
                {
                    try
                    {
                        Logger.Log("ModManagerPage: Loaded start");
                        var translations = _translation.LoadAsync();
                        await LoadDirectoryAsync(_currentDir);
                        await translations;
                        Logger.Log("ModManagerPage: Translation loaded OK");
                        Logger.Log("ModManagerPage: Directory loaded OK");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"ModManagerPage: Loaded FAILED: {ex}");
                        try
                        {
                            ThemedDialog.Show(Window.GetWindow(this),
                                $"Mod Manager Error:\n\n{ex.Message}\n\n{ex.StackTrace}",
                                "Error");
                        }
                        catch { }
                    }
                };

                Logger.Log("ModManagerPage: ctor end OK");
            }
            catch (Exception ex)
            {
                Logger.Log($"ModManagerPage: ctor CRASH: {ex}");
                try
                {
                    var msg = $"ModManagerPage 构造崩溃：\n\n{ex.Message}\n\n{ex.GetType().Name}\n\n{ex.StackTrace}";
                    MessageBox.Show(msg, "CRASH", MessageBoxButton.OK, MessageBoxImage.Error);

                    var logDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ETBTool");
                    Directory.CreateDirectory(logDir);
                    File.WriteAllText(
                        Path.Combine(logDir, "modmanager_crash.txt"),
                        $"[{DateTime.Now}]\n{ex}");
                }
                catch { }
            }
        }

        // ★ 翻译更新后刷新已有条目
        private void OnTranslationsChanged()
        {
            try
            {
                TranslationState.Text = _translation.StatusMessage;
                foreach (var entry in _entries)
                {
                    {
                        var trans = _translation.GetTranslation(entry.FileName);
                        entry.DisplayName = trans ?? entry.FileName;
                        entry.OnChanged(nameof(entry.DisplayName));
                    }
                }
            }
            catch { }
        }

        private void RefreshText()
        {
            try
            {
                if (PageTitle != null) PageTitle.Text = LanguageManager.ModMgrTitle;
                if (PageSub != null) PageSub.Text = LanguageManager.ModMgrSub;
                if (LblPath != null) LblPath.Text = LanguageManager.ModMgrPath;
                if (BtnOpen != null) BtnOpen.Content = LanguageManager.ModMgrOpenFolder;
                if (BtnCopy != null) BtnCopy.Content = LanguageManager.ModMgrCopyPath;
                if (LblList != null) LblList.Text = LanguageManager.ModMgrList;
                if (LoadingHint != null) LoadingHint.Text = LanguageManager.ModMgrLoading;
                if (HStatus != null) HStatus.Text = LanguageManager.ModMgrHStatus;
                if (HName != null) HName.Text = LanguageManager.ModMgrHName;
                if (HFile != null) HFile.Text = LanguageManager.ModMgrHFile;
                if (HSize != null) HSize.Text = LanguageManager.ModMgrHSize;
                if (HMod != null) HMod.Text = LanguageManager.ModMgrHMod;
                if (BtnUp != null) BtnUp.Content = LanguageManager.ModMgrUp;
                if (BtnRefresh != null) BtnRefresh.Content = LanguageManager.ModMgrRefresh;
        if (BtnSubmitName != null) BtnSubmitName.Content = LanguageManager.IsChinese ? "提交 Mod 名称" : "Submit mod name";
                if (BtnImport != null) BtnImport.Content = LanguageManager.ModMgrImport;
                if (BtnPack != null) BtnPack.Content = LanguageManager.ModMgrPack;
                if (BtnDel != null) BtnDel.Content = LanguageManager.ModMgrDelete;
                if (LblActions != null) LblActions.Text = LanguageManager.ModMgrActions;
                UpdateToggleBtn();
                TranslationState.Text = _translation.StatusMessage;
            }
            catch (Exception ex) { Logger.Log($"RefreshText: {ex.Message}"); }
        }

        private void UpdateToggleBtn()
        {
            try
            {
                if (ToggleBtn == null) return;
                var selected = ModListBox.SelectedItems.Cast<ModEntry>().ToArray();
                ToggleBtn.Content = selected.Length > 0 && selected.All(entry => entry.IsEnabled)
                    ? LanguageManager.ModMgrToggleDisable
                    : LanguageManager.ModMgrToggleEnable;
                ToggleBtn.IsEnabled = !_operating && selected.Length > 0;
                BtnPack.IsEnabled = !_operating && selected.Length > 0;
            }
            catch { }
        }

        private async Task LoadDirectoryAsync(string path)
        {
            var generation = ++_loadGeneration;
            var selectedPaths = ModListBox.SelectedItems.Cast<ModEntry>().Select(entry => entry.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
            try
            {
                _entries.Clear();
                if (LoadingHint != null) LoadingHint.Visibility = Visibility.Visible;
                if (CurrentDirText != null) CurrentDirText.Text = path ?? "";

                if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    if (LoadingHint != null) LoadingHint.Visibility = Visibility.Collapsed;
                    return;
                }

                await Task.Run(() =>
                {
                    var list = new List<ModEntry>();
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(path))
                        {
                            try
                            {
                                var info = new DirectoryInfo(dir);
                                var disabled = info.Name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                                list.Add(new ModEntry
                                {
                                    DisplayName = info.Name,
                                    FileName = ModFileOperations.DisplayFileName(info.Name),
                                    Size = "-",
                                    Modified = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                                    Status = disabled ? "OFF" : "ON",
                                    StatusColor = ModEntry.GetStatusBrush(disabled),
                                    FullPath = dir,
                                    IsDirectory = true,
                                    IsEnabled = !disabled
                                });
                            }
                            catch (Exception ex) { Logger.Log($"ScanDir item: {ex.Message}"); }
                        }

                        foreach (var file in Directory.GetFiles(path))
                        {
                            try
                            {
                                var info = new FileInfo(file);
                                if (string.Equals(info.Name, "EscapeTheBackrooms-WindowsNoEditor.pak", StringComparison.OrdinalIgnoreCase))
                                    continue;
                                var disabled = info.Name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
                                list.Add(new ModEntry
                                {
                                    DisplayName = info.Name,
                                    FileName = ModFileOperations.DisplayFileName(info.Name),
                                    Size = FormatSize(info.Length),
                                    Modified = info.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                                    Status = disabled ? "OFF" : "ON",
                                    StatusColor = ModEntry.GetStatusBrush(disabled),
                                    FullPath = file,
                                    IsDirectory = false,
                                    IsEnabled = !disabled
                                });
                            }
                            catch (Exception ex) { Logger.Log($"ScanFile item: {ex.Message}"); }
                        }
                    }
                    catch (Exception ex) { Logger.Log($"ScanDir: {ex.Message}"); }

                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        if (generation != _loadGeneration) return;
                        try
                        {
                            foreach (var e in list)
                            {
                                {
                                    var trans = _translation.GetTranslation(e.FileName);
                                    e.DisplayName = trans ?? e.FileName;
                                }
                                _entries.Add(e);
                                if (selectedPaths.Contains(e.FullPath)) ModListBox.SelectedItems.Add(e);
                            }
                        }
                        catch (Exception ex) { Logger.Log($"EntriesAdd: {ex.Message}"); }
                    });
                });
            }
            catch (Exception ex) { Logger.Log($"LoadDir: {ex.Message}"); }
            finally
            {
                try { if (LoadingHint != null) LoadingHint.Visibility = Visibility.Collapsed; } catch { }
            }
        }

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1048576 => $"{bytes / 1024.0:F1} KB",
            < 1073741824L => $"{bytes / 1048576.0:F1} MB",
            _ => $"{bytes / 1073741824.0:F2} GB"
        };

        private void OpenRoot(object s, RoutedEventArgs e)
        {
            ExternalLauncher.OpenFolder(_currentDir);
        }

        private void CopyRoot(object s, RoutedEventArgs e)
        {
            try { Clipboard.SetText(_currentDir); Toast.Show(LanguageManager.MsgCopied, ToastType.Success); }
            catch { }
        }

        private void ModSel(object s, SelectionChangedEventArgs e)
        {
            try { UpdateToggleBtn(); } catch { }
        }

        private async void ModDblClick(object s, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var sel = ModListBox.SelectedItem as ModEntry;
                if (sel == null) return;
                if (sel.IsDirectory)
                {
                    _currentDir = sel.FullPath;
                    if (ModPathBox != null) ModPathBox.Text = _currentDir;
                    await LoadDirectoryAsync(_currentDir);
                }
                else ExternalLauncher.OpenFile(sel.FullPath);
            }
            catch (Exception ex) { Logger.Log($"ModDblClick: {ex.Message}"); }
        }

        private async void GoUp(object s, RoutedEventArgs e)
        {
            try
            {
                var parent = Path.GetDirectoryName(_currentDir);
                if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                {
                    _currentDir = parent;
                    if (ModPathBox != null) ModPathBox.Text = _currentDir;
                    await LoadDirectoryAsync(_currentDir);
                }
            }
            catch { }
        }

        private async void Refresh(object s, RoutedEventArgs e)
        {
            if (_operating) return;
            var translations = _translation.LoadAsync();
            await LoadDirectoryAsync(_currentDir);
            await translations;
        }

        private async void Import(object s, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog { Filter = "ZIP|*.zip|PAK|*.pak|All|*.*", Multiselect = true };
                if (dlg.ShowDialog() != true) return;
                foreach (var file in dlg.FileNames)
                {
                    var ext = Path.GetExtension(file).ToLower();
                    if (ext == ".zip") ZipFile.ExtractToDirectory(file, _currentDir, true);
                    else
                    {
                        var dest = Path.Combine(_currentDir, Path.GetFileName(file));
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Copy(file, dest);
                    }
                }
                Toast.Show(LanguageManager.ModMgrImportSuccess, ToastType.Success);
                await LoadDirectoryAsync(_currentDir);
            }
            catch (Exception ex)
            {
                ThemedDialog.Show(Window.GetWindow(this), LanguageManager.ModMgrImportFailed(ex.Message), LanguageManager.ModMgrImportTitle);
            }
        }

        private async void Pack(object s, RoutedEventArgs e)
        {
            if (_operating) return;
            var selected = ModListBox.SelectedItems.Cast<ModEntry>().ToArray();
            if (selected.Length == 0) { Toast.Show(LanguageManager.ModMgrNoSelection, ToastType.Info); return; }
            var dialog = new SaveFileDialog { FileName = selected.Length == 1 ? selected[0].FileName + ".zip" : "Mods.zip", Filter = "ZIP|*.zip", DefaultExt = ".zip" };
            if (dialog.ShowDialog() != true) return;
            SetOperating(true);
            try
            {
                await Task.Run(() => ModFileOperations.Pack(selected.Select(entry => entry.FullPath), dialog.FileName));
                Logger.Log($"模组打包完成，共选中 {selected.Length} 项：{dialog.FileName}");
                Toast.Show(LanguageManager.ModMgrPackSuccess, ToastType.Success);
            }
            catch (Exception ex)
            {
                Logger.Log($"模组打包失败: {ex}");
                ThemedDialog.Show(Window.GetWindow(this), LanguageManager.ModMgrPackFailed(Logger.NormalizeForDisplay(ex.Message)), LanguageManager.ModMgrPackTitle);
            }
            finally { SetOperating(false); }
        }

        private void SetOperating(bool value)
        {
            _operating = value;
            ModListBox.IsEnabled = !value;
            BtnRefresh.IsEnabled = BtnUp.IsEnabled = BtnImport.IsEnabled = BtnDel.IsEnabled = !value;
            UpdateToggleBtn();
        }

        private async void Del(object s, RoutedEventArgs e)
        {
            try
            {
                var sel = ModListBox.SelectedItem as ModEntry;
                if (sel == null) { Toast.Show(LanguageManager.ModMgrNoSelection, ToastType.Info); return; }
                var result = ThemedDialog.Show(Window.GetWindow(this), LanguageManager.ModMgrDeleteConfirm(sel.FileName), LanguageManager.ModMgrDeleteTitle, MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;
                if (sel.IsDirectory) Directory.Delete(sel.FullPath, true); else File.Delete(sel.FullPath);
                Toast.Show(LanguageManager.ModMgrDeleteSuccess, ToastType.Success);
                await LoadDirectoryAsync(_currentDir);
            }
            catch (Exception ex) { ThemedDialog.Show(Window.GetWindow(this), LanguageManager.ModMgrDeleteFailed(ex.Message), LanguageManager.ModMgrDeleteTitle); }
        }

        private void SubmitName(object s, RoutedEventArgs e)
        {
            ExternalLauncher.OpenWebsite("https://acnq9mbub8ek.feishu.cn/share/base/form/shrcnU6cBUdmrLjyKxltPlcwweg");
        }

        private async void Toggle(object s, RoutedEventArgs e)
        {
            if (_operating) return;
            var selected = ModListBox.SelectedItems.Cast<ModEntry>().ToArray();
            if (selected.Length == 0) { Toast.Show(LanguageManager.ModMgrNoSelection, ToastType.Info); return; }
            bool enable = !selected.All(entry => entry.IsEnabled);
            SetOperating(true);
            try
            {
                var after = await Task.Run(() => ModFileOperations.Toggle(selected.Select(entry => entry.FullPath), enable));
                Logger.Log($"模组状态切换完成：{(enable ? "启用" : "禁用")}，共选中 {selected.Length} 项");
                await LoadDirectoryAsync(_currentDir);
                foreach (var entry in _entries)
                    if (after.Values.Contains(entry.FullPath, StringComparer.OrdinalIgnoreCase) && !ModListBox.SelectedItems.Contains(entry))
                        ModListBox.SelectedItems.Add(entry);
                Toast.Show(LanguageManager.ModMgrToggleSuccess(enable), ToastType.Success);
            }
            catch (Exception ex)
            {
                Logger.Log($"模组状态切换失败: {ex}");
                ThemedDialog.Show(Window.GetWindow(this), Logger.NormalizeForDisplay(ex.Message), "模组操作失败");
                await LoadDirectoryAsync(_currentDir);
            }
            finally { SetOperating(false); }
        }
    }
}
