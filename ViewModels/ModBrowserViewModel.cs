using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using ETBTool.Models;
using ETBTool.Services;
using ETBTool.Utils;
using ETBTool.Windows;
using Microsoft.Win32;

namespace ETBTool.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? n = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value; OnPropertyChanged(n); return true;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _can;
        public RelayCommand(Action<object?> execute, Predicate<object?>? can = null) { _execute = execute; _can = can; }
        public bool CanExecute(object? p) => _can?.Invoke(p) ?? true;
        public void Execute(object? p) => _execute(p);
        public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _can;
        private bool _running;
        public AsyncRelayCommand(Func<Task> execute, Func<bool>? can = null) { _execute = execute; _can = can; }
        public bool CanExecute(object? p) => !_running && (_can?.Invoke() ?? true);
        public async void Execute(object? p)
        {
            if (_running) return;
            _running = true;
            CommandManager.InvalidateRequerySuggested();
            try { await _execute(); }
            finally { _running = false; CommandManager.InvalidateRequerySuggested(); }
        }
        public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
    }

    public class DownloadTask : INotifyPropertyChanged
    {
        public int ModId { get; set; }
        public CancellationTokenSource? Cts { get; set; }
        private string _name = "", _status = "";
        private float _progress;
        private bool _showCancel = true, _showDismiss;
        public string ModName { get => _name; set { _name = value; OnPropertyChanged(); } }
        public float Progress { get => _progress; set { _progress = value; OnPropertyChanged(); } }
        public string StatusText { get => _status; set { _status = value; OnPropertyChanged(); } }
        public bool ShowCancel { get => _showCancel; set { _showCancel = value; OnPropertyChanged(); } }
        public bool ShowDismiss { get => _showDismiss; set { _showDismiss = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class ModBrowserViewModel : ViewModelBase
    {
        private readonly ModApiClient _api = new();
        private string? _token;

        private string _la = "", _lp = "", _sm = "";
        public string LoginAccount { get => _la; set => Set(ref _la, value); }
        public string LoginPassword { get => _lp; set => Set(ref _lp, value); }
        public string StatusMessage { get => _sm; set => Set(ref _sm, value); }

        private string _re = "", _rc = "", _ru = "", _rp = "";
        private string _cbt = LanguageManager.ModRegSendCode;
        private bool _cbe = true;
        public string RegEmail { get => _re; set => Set(ref _re, value); }
        public string RegCode { get => _rc; set => Set(ref _rc, value); }
        public string RegUsername { get => _ru; set => Set(ref _ru, value); }
        public string RegPassword { get => _rp; set => Set(ref _rp, value); }
        public string CodeButtonText { get => _cbt; set => Set(ref _cbt, value); }
        public bool CodeButtonEnabled { get => _cbe; set => Set(ref _cbe, value); }

        private bool _sl = true, _sr, _sb, _sd, _sa, _il;
        private string _st = "", _cu = "";
        public bool ShowLogin { get => _sl; set => Set(ref _sl, value); }
        public bool ShowRegister { get => _sr; set => Set(ref _sr, value); }
        public bool ShowBrowse { get => _sb; set => Set(ref _sb, value); }
        public bool ShowDetail { get => _sd; set => Set(ref _sd, value); }
        public bool ShowAbout { get => _sa; set => Set(ref _sa, value); }
        public bool IsLoading { get => _il; set => Set(ref _il, value); }
        public string StatsText { get => _st; set => Set(ref _st, value); }
        public string CurrentUser { get => _cu; set => Set(ref _cu, value); }

        private string _se = "", _cat = "all";
        private int _cp = 1, _tp = 1;
        private ObservableCollection<ModItem> _mods = new();
        private ModItem? _dm;
        private ObservableCollection<DownloadTask> _dl = new();

        public string SearchText { get => _se; set => Set(ref _se, value); }
        public int CurrentPage { get => _cp; set { if (Set(ref _cp, value)) { OnPropertyChanged(nameof(CanPrev)); OnPropertyChanged(nameof(CanNext)); } } }
        public int TotalPages { get => _tp; set { if (Set(ref _tp, value)) { OnPropertyChanged(nameof(CanPrev)); OnPropertyChanged(nameof(CanNext)); } } }
        public bool CanPrev => CurrentPage > 1;
        public bool CanNext => CurrentPage < TotalPages;
        public ObservableCollection<ModItem> Mods { get => _mods; set => Set(ref _mods, value); }
        public ObservableCollection<DownloadTask> Downloads { get => _dl; set => Set(ref _dl, value); }
        public bool HasDownloads => Downloads.Count > 0;

        private DownloadTask? _adt;
        public DownloadTask? ActiveDetailDownload { get => _adt; set { Set(ref _adt, value); OnPropertyChanged(nameof(HasDetailDownload)); } }
        public bool HasDetailDownload => ActiveDetailDownload != null;

        public ModItem? DetailMod { get => _dm; set { Set(ref _dm, value); UpdateDetailDownload(); } }
        private void UpdateDetailDownload()
        {
            ActiveDetailDownload = DetailMod != null ? Downloads.FirstOrDefault(t => t.ModId == DetailMod.Id) : null;
        }

        public ICommand LoginCmd { get; }
        public ICommand GoRegisterCmd { get; }
        public ICommand GoLoginCmd { get; }
        public ICommand SendCodeCmd { get; }
        public ICommand RegisterCmd { get; }
        public ICommand LogoutCmd { get; }
        public ICommand SetCategoryCmd { get; }
        public ICommand SearchCmd { get; }
        public ICommand PrevPageCmd { get; }
        public ICommand NextPageCmd { get; }
        public ICommand OpenDetailCmd { get; }
        public ICommand DownloadCmd { get; }
        public ICommand DownloadDirectCmd { get; }
        public ObservableCollection<BitmapImage> DetailImages { get; } = new();
        public ICommand CancelDlCmd { get; }
        public ICommand DismissDlCmd { get; }
        public ICommand ClearAllDlCmd { get; }

        public ModBrowserViewModel()
        {
            LoginCmd = new AsyncRelayCommand(LoginAsync);
            GoRegisterCmd = new RelayCommand(_ => { ShowLogin = false; ShowRegister = true; StatusMessage = ""; });
            GoLoginCmd = new RelayCommand(_ => { ShowRegister = false; ShowLogin = true; StatusMessage = ""; });
            SendCodeCmd = new AsyncRelayCommand(SendCodeAsync, () => CodeButtonEnabled);
            RegisterCmd = new AsyncRelayCommand(RegisterAsync);
            LogoutCmd = new RelayCommand(_ =>
            {
                _token = null; _api.SetToken(null); SaveToken();
                ShowBrowse = false; ShowLogin = true; CurrentUser = ""; Mods.Clear();
            });
            SetCategoryCmd = new RelayCommand(p => { _cat = p as string ?? "all"; CurrentPage = 1; _ = LoadModsAsync(); });
            SearchCmd = new AsyncRelayCommand(() => { CurrentPage = 1; return LoadModsAsync(); });
            PrevPageCmd = new RelayCommand(_ => { if (CanPrev) { CurrentPage--; _ = LoadModsAsync(); } }, _ => CanPrev);
            NextPageCmd = new RelayCommand(_ => { if (CanNext) { CurrentPage++; _ = LoadModsAsync(); } }, _ => CanNext);
            OpenDetailCmd = new RelayCommand(p =>
            {
                int id = p is int i ? i : int.TryParse(p?.ToString(), out var v) ? v : 0;
                if (id > 0) _ = LoadDetailAsync(id);
            });
            DownloadCmd = new AsyncRelayCommand(DownloadModAsync);
            DownloadDirectCmd = new AsyncRelayCommand(OpenDirectInBrowserAsync);
            CancelDlCmd = new RelayCommand(p => { if (p is DownloadTask t) t.Cts?.Cancel(); });
            DismissDlCmd = new RelayCommand(p =>
            {
                if (p is DownloadTask t) { Downloads.Remove(t); OnPropertyChanged(nameof(HasDownloads)); UpdateDetailDownload(); }
            });
            ClearAllDlCmd = new RelayCommand(_ =>
            {
                Downloads.Clear(); OnPropertyChanged(nameof(HasDownloads)); ActiveDetailDownload = null;
            });
            LoadToken();
            LoadSettings();
        }

        // ═══ Settings ═══

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(GamePaths.SettingsFile)) return;
                using var doc = JsonDocument.Parse(File.ReadAllText(GamePaths.SettingsFile));
            }
            catch { }
        }

        private void SaveFlag(string key, string value)
        {
            try
            {
                var dict = new Dictionary<string, JsonElement>();
                if (File.Exists(GamePaths.SettingsFile))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(GamePaths.SettingsFile));
                    foreach (var p in doc.RootElement.EnumerateObject())
                        dict[p.Name] = p.Value.Clone();
                }
                dict[key] = JsonSerializer.SerializeToElement(value);
                Directory.CreateDirectory(Path.GetDirectoryName(GamePaths.SettingsFile)!);
                File.WriteAllText(GamePaths.SettingsFile,
                    JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        // ═══ Token ═══

        private void LoadToken()
        {
            try
            {
                if (!File.Exists(GamePaths.SettingsFile)) return;
                using var doc = JsonDocument.Parse(File.ReadAllText(GamePaths.SettingsFile));
                if (doc.RootElement.TryGetProperty("ModToken", out var t) && t.ValueKind == JsonValueKind.String)
                {
                    var tk = t.GetString();
                    if (!string.IsNullOrEmpty(tk)) { _token = tk; _api.SetToken(tk); }
                }
            }
            catch { }
        }

        private void SaveToken()
        {
            try
            {
                var dict = new Dictionary<string, JsonElement>();
                if (File.Exists(GamePaths.SettingsFile))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(GamePaths.SettingsFile));
                    foreach (var p in doc.RootElement.EnumerateObject())
                        dict[p.Name] = p.Value.Clone();
                }
                dict["ModToken"] = JsonSerializer.SerializeToElement(_token ?? "");
                Directory.CreateDirectory(Path.GetDirectoryName(GamePaths.SettingsFile)!);
                File.WriteAllText(GamePaths.SettingsFile,
                    JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private void RestoreUsernameFromToken()
        {
            if (string.IsNullOrEmpty(_token)) return;
            try
            {
                var parts = _token.Split('.');
                if (parts.Length == 3)
                {
                    var payload = parts[1];
                    payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("username", out var u) && u.ValueKind == JsonValueKind.String)
                    {
                        var name = u.GetString();
                        if (!string.IsNullOrEmpty(name)) CurrentUser = $"@{name}";
                    }
                }
            }
            catch { }
        }

        // ═══ Init ═══

        public async Task InitAsync()
        {
            if (string.IsNullOrEmpty(_token)) return;
            try
            {
                RestoreUsernameFromToken();
                ShowLogin = false; ShowBrowse = true;
                await LoadStatsAsync(); await LoadModsAsync();
            }
            catch { _token = null; _api.SetToken(null); ShowLogin = true; ShowBrowse = false; }
        }

        // ═══ Login ═══

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(LoginAccount) || string.IsNullOrWhiteSpace(LoginPassword))
            { StatusMessage = LanguageManager.ModLoginFillAll; return; }
            StatusMessage = "";
            try
            {
                var resp = await _api.PostJsonAsync<LoginResponse>("/user/login",
                    new { account = LoginAccount, password = LoginPassword });
                if (resp == null) { StatusMessage = "Unknown error"; return; }
                if (resp.Banned) { StatusMessage = LanguageManager.ModLoginBanned(resp.BanReason ?? ""); return; }
                if (!string.IsNullOrEmpty(resp.Error)) { StatusMessage = resp.Error; return; }
                if (string.IsNullOrEmpty(resp.Token)) { StatusMessage = resp.Error ?? "Login failed"; return; }
                _token = resp.Token; _api.SetToken(_token); SaveToken();
                CurrentUser = $"@{resp.User.Username}";
                ShowLogin = false; ShowBrowse = true;
                await LoadStatsAsync(); await LoadModsAsync();
            }
            catch (Exception ex) { StatusMessage = $"Login failed: {ex.Message}"; Logger.Log($"Login: {ex.Message}"); }
        }

        // ═══ Register ═══

        private async Task SendCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(RegEmail)) { StatusMessage = LanguageManager.ModRegFillAll; return; }
            try
            {
                CodeButtonEnabled = false;
                await _api.PostJsonAsync<MessageResponse>("/user/send-code", new { email = RegEmail, type = "register" });
                int sec = 60; CodeButtonText = $"{sec}s";
                var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                t.Tick += (_, _) =>
                {
                    sec--;
                    if (sec <= 0) { t.Stop(); CodeButtonText = LanguageManager.ModRegSendCode; CodeButtonEnabled = true; }
                    else CodeButtonText = $"{sec}s";
                };
                t.Start();
            }
            catch (Exception ex) { StatusMessage = $"Send code failed: {ex.Message}"; CodeButtonEnabled = true; }
        }

        private async Task RegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(RegEmail) || string.IsNullOrWhiteSpace(RegCode) ||
                string.IsNullOrWhiteSpace(RegUsername) || string.IsNullOrWhiteSpace(RegPassword))
            { StatusMessage = LanguageManager.ModRegFillAll; return; }
            try
            {
                var resp = await _api.PostJsonAsync<MessageResponse>("/user/register",
                    new { email = RegEmail, code = RegCode, username = RegUsername, password = RegPassword });
                if (resp?.Error != null) { StatusMessage = resp.Error; return; }
                StatusMessage = LanguageManager.ModRegSuccess; ShowRegister = false; ShowLogin = true;
            }
            catch (Exception ex) { StatusMessage = $"Register failed: {ex.Message}"; }
        }

        // ═══ Load Data ═══

        private async Task LoadStatsAsync()
        {
            try
            {
                var s = await _api.GetAsync<StatsResponse>("/stats");
                if (s != null) StatsText = LanguageManager.ModStatsText(s.Users, s.Mods, s.Tools);
            }
            catch { }
        }

        private async Task LoadModsAsync()
        {
            IsLoading = true;
            try
            {
                var ep = $"/mods?page={CurrentPage}&per_page=20";
                if (_cat != "all") ep += $"&category={_cat}";
                if (!string.IsNullOrWhiteSpace(SearchText)) ep += $"&search={Uri.EscapeDataString(SearchText)}";
                var resp = await _api.GetAsync<ModListResponse>(ep);
                if (resp == null) throw new InvalidDataException("模组库返回了无效列表");
                Mods = new ObservableCollection<ModItem>(resp.Mods);
                TotalPages = Math.Max(1, resp.Pages);
                CurrentPage = Math.Clamp(resp.Page, 1, TotalPages);
                StatusMessage = resp.Mods.Count == 0 ? (LanguageManager.IsChinese ? "没有符合条件的模组" : "No matching mods") : "";
            }
            catch (Exception ex)
            {
                Logger.Log($"LoadMods: {ex.Message}");
                Mods.Clear();
                StatusMessage = LanguageManager.IsChinese ? "无法连接模组库，请检查网络后重试" : "Cannot reach the catalogue. Check your connection and retry.";
                Toast.Show(StatusMessage, ToastType.Error);
            }
            finally { IsLoading = false; }
        }

        private async Task LoadDetailAsync(int id)
        {
            try
            {
                var m = await _api.GetAsync<ModItem>($"/mods/{id}");
                if (m != null) { DetailMod = m; ShowDetail = true; _ = LoadDetailImagesAsync(m); }
            }
            catch { }
        }

        // ═══ Download ═══

        private async Task OpenExternalStorageAsync()
        {
            if (DetailMod == null) return;
            var message = LanguageManager.IsChinese
                ? "本模组由上传者自提供存储方式，需要唤起浏览器，无法实现本地下载。\n\n是否现在打开浏览器访问上传者提供的下载页面？"
                : "This mod is hosted by its uploader. It requires opening your browser and cannot be downloaded locally.\n\nOpen the uploader's download page now?";
            var title = LanguageManager.IsChinese ? "上传者自提供下载" : "Uploader-hosted download";
            if (ThemedDialog.Show(Application.Current?.MainWindow, message, title, MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                var url = await _api.GetDownloadUrlAsync(DetailMod.Id);
                if (string.IsNullOrWhiteSpace(url)) throw new InvalidDataException("上传者未提供有效的下载地址");
                if (!ExternalLauncher.OpenWebsite(url)) throw new InvalidOperationException("无法打开浏览器");
                Toast.Show(LanguageManager.IsChinese ? "已在浏览器中打开上传者的下载页面" : "Opened the uploader's page in your browser", ToastType.Success);
            }
            catch (Exception ex)
            {
                Logger.Log($"外部存储下载失败：{ex.Message}");
                Toast.Show(LanguageManager.IsChinese ? "打开下载页面失败：" + ex.Message : "Failed to open the download page: " + ex.Message, ToastType.Error);
            }
        }

        private async Task OpenDirectInBrowserAsync()
        {
            if (DetailMod == null) return;
            if (string.IsNullOrEmpty(_token)) { Toast.Show(LanguageManager.ModLoginExpired, ToastType.Error); return; }
            if (DetailMod.IsDisabled) { Toast.Show(LanguageManager.ModDetailDisabled, ToastType.Error); return; }
            if (string.Equals(DetailMod.StorageType, "external", StringComparison.OrdinalIgnoreCase)) { await OpenExternalStorageAsync(); return; }
            var message = LanguageManager.IsChinese
                ? "直链下载会打开系统默认浏览器，由浏览器从 123 云盘直接下载文件。\n\n软件本身不会下载该文件，是否继续？"
                : "Direct download opens your default browser and downloads from the storage provider.\n\nETBTool itself will not download the file. Continue?";
            var title = LanguageManager.IsChinese ? "直链下载" : "Direct download";
            if (ThemedDialog.Show(Application.Current?.MainWindow, message, title, MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                var url = await _api.GetDownloadUrlAsync(DetailMod.Id, "direct");
                if (string.IsNullOrWhiteSpace(url)) throw new InvalidDataException("直链接口未返回地址");
                if (!ExternalLauncher.OpenWebsite(url)) return;
                Toast.Show(LanguageManager.IsChinese ? "已在浏览器中打开直链下载" : "Opened in your browser", ToastType.Success);
            }
            catch (Exception ex)
            {
                Logger.Log($"直链下载失败：{ex.Message}");
                Toast.Show(LanguageManager.IsChinese ? "直链下载失败：" + ex.Message : "Direct download failed: " + ex.Message, ToastType.Error);
            }
        }

        private Task DownloadModAsync() => StartDownloadAsync(false);

        private async Task StartDownloadAsync(bool direct)
        {
            if (DetailMod == null) return;
            if (string.IsNullOrEmpty(_token)) { Toast.Show(LanguageManager.ModLoginExpired, ToastType.Error); return; }
            if (DetailMod.IsDisabled) { Toast.Show(LanguageManager.ModDetailDisabled, ToastType.Error); return; }
            if (string.Equals(DetailMod.StorageType, "external", StringComparison.OrdinalIgnoreCase)) { await OpenExternalStorageAsync(); return; }

            var existing = Downloads.FirstOrDefault(t => t.ModId == DetailMod.Id);
            if (existing != null && existing.ShowCancel) { Toast.Show(LanguageManager.ModDlAlready, ToastType.Info); return; }

            if (DetailMod.Dependencies.Count > 0)
            {
                var depNames = string.Join("\n", DetailMod.Dependencies.Select(d => $"  \u2022 {d.DisplayName}"));
                var depResult = ThemedDialog.Show(Application.Current?.MainWindow, LanguageManager.ModDetailDepWarn(depNames), LanguageManager.ModDetailDeps, MessageBoxButton.YesNo);
                if (depResult != MessageBoxResult.Yes) return;
            }

            var tempDir = Path.Combine(GamePaths.AppConfigPath, "temp_downloads");
            Directory.CreateDirectory(tempDir);
            var notice = LanguageManager.IsChinese
                ? $"文件会先下载到临时目录：\n{tempDir}\n\n完成后你可以选择：\n\u2022 自动安装到模组文件夹\n\u2022 另存到其他位置\n\u2022 保留在临时目录"
                : $"The file will be downloaded to:\n{tempDir}\n\nAfter that you can install it, save it elsewhere, or keep it in the temp folder.";
            var noticeTitle = direct ? (LanguageManager.IsChinese ? "直链下载" : "Direct download") : LanguageManager.ModDetailDownload;
            if (ThemedDialog.Show(Application.Current?.MainWindow, notice, noticeTitle, MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;

            var task = new DownloadTask
            {
                ModId = DetailMod.Id,
                ModName = DetailMod.Name,
                StatusText = LanguageManager.ModDlPreDownloading,
                Cts = new CancellationTokenSource()
            };
            Downloads.Add(task);
            OnPropertyChanged(nameof(HasDownloads));
            UpdateDetailDownload();

            try
            {
                var capturedTask = task;
                var prog = new Progress<DownloadProgress>(p =>
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        capturedTask.Progress = p.Percent >= 0 ? p.Percent : 0;
                        capturedTask.StatusText = p.StatusText;
                    }));

                var file = await _api.DownloadFileAsync(task.ModId, tempDir, prog, task.Cts!.Token, direct ? "direct" : "local");
                task.StatusText = LanguageManager.ModDlChecking;
                task.Progress = -1;

                var ext = Path.GetExtension(file).ToLowerInvariant();
                var isPak = ext == ".pak";
                var isZip = ext == ".zip";
                var question = LanguageManager.IsChinese
                    ? (isPak
                        ? $"文件已下载到临时目录：\n{file}\n\n要现在复制到模组文件夹（Paks）吗？\n\n是：自动安装\n否：另存到其他位置\n取消：保留在临时目录"
                        : isZip
                            ? $"压缩包已下载到临时目录：\n{file}\n\n是否直接解压并安装到模组文件夹？\n\n是：解压安装\n否：另存到其他位置\n取消：保留在临时目录"
                            : $"文件已下载到临时目录：\n{file}\n\n该格式无法自动安装。\n\n否：另存到其他位置\n取消：保留在临时目录")
                    : (isPak ? $"Downloaded to:\n{file}\n\nCopy it into the mods folder now?\n\nYes: install\nNo: save elsewhere\nCancel: keep in temp"
                        : isZip ? $"Archive downloaded to:\n{file}\n\nExtract and install into the mods folder?\n\nYes: install\nNo: save elsewhere\nCancel: keep in temp"
                        : $"Downloaded to:\n{file}\n\nThis format cannot be installed automatically.\n\nNo: save elsewhere\nCancel: keep in temp");
                var title = LanguageManager.IsChinese ? "下载完成" : "Download finished";
                var choice = ThemedDialog.Show(Application.Current?.MainWindow, question, title, MessageBoxButton.YesNoCancel);

                if (choice == MessageBoxResult.Yes && isPak)
                {
                    Directory.CreateDirectory(GamePaths.ModPath);
                    var target = Path.Combine(GamePaths.ModPath, Path.GetFileName(file));
                    File.Copy(file, target, true);
                    try { File.Delete(file); } catch { }
                    task.Progress = 100;
                    task.StatusText = LanguageManager.ModDlInstalled;
                    Toast.Show(LanguageManager.ModDlInstalled, ToastType.Success);
                }
                else if (choice == MessageBoxResult.Yes && isZip)
                {
                    var logicMods = Path.Combine(GamePaths.ModPath, "LogicMods");
                    Directory.CreateDirectory(logicMods);
                    ZipFile.ExtractToDirectory(file, logicMods, true);
                    try { File.Delete(file); } catch { }
                    task.Progress = 100;
                    task.StatusText = LanguageManager.ModDlInstalled;
                    Toast.Show(LanguageManager.ModDlInstalled, ToastType.Success);
                }
                else if (choice == MessageBoxResult.No)
                {
                    var dlg = new SaveFileDialog { FileName = Path.GetFileName(file), Filter = isZip ? "ZIP|*.zip|All|*.*" : "All|*.*" };
                    if (dlg.ShowDialog() == true)
                    {
                        if (File.Exists(dlg.FileName)) File.Delete(dlg.FileName);
                        File.Move(file, dlg.FileName);
                        task.Progress = 100;
                        task.StatusText = $"{LanguageManager.ModDlSaved}: {Path.GetFileName(dlg.FileName)}";
                        Toast.Show(LanguageManager.ModDlSaved, ToastType.Success);
                    }
                    else
                    {
                        task.StatusText = LanguageManager.IsChinese ? $"已保留在临时目录：{file}" : $"Kept in temp: {file}";
                        Toast.Show(task.StatusText, ToastType.Info);
                    }
                }
                else
                {
                    task.StatusText = LanguageManager.IsChinese ? $"已保留在临时目录：{file}" : $"Kept in temp: {file}";
                    Toast.Show(task.StatusText, ToastType.Info);
                }

                task.ShowCancel = false;
                task.ShowDismiss = true;
            }
            catch (OperationCanceledException)
            {
                Downloads.Remove(task);
                OnPropertyChanged(nameof(HasDownloads));
                UpdateDetailDownload();
                Toast.Show(LanguageManager.ModDlCancelled, ToastType.Info);
            }
            catch (Exception ex)
            {
                task.StatusText = LanguageManager.ModDlFailed;
                task.ShowCancel = false;
                task.ShowDismiss = true;
                Logger.Log($"Download: {ex.Message}");
                Toast.Show(LanguageManager.ModDlNetworkError(ex.Message), ToastType.Error);
            }
        }

        private async Task LoadDetailImagesAsync(ModItem mod)
        {
            DetailImages.Clear();
            foreach (var image in mod.Images.Take(8))
            {
                try
                {
                    var url = await _api.GetImageUrlAsync(image.Id);
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    var bytes = await _api.DownloadImageBytesAsync(url);
                    if (bytes == null || bytes.Length == 0) continue;
                    var bitmap = new BitmapImage();
                    using (var stream = new MemoryStream(bytes))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }
                    DetailImages.Add(bitmap);
                }
                catch (Exception ex) { Logger.Log($"预览图加载失败：{ex.Message}"); }
            }
        }

        private static bool IsModArchive(string zipPath)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                foreach (var entry in archive.Entries)
                {
                    var ext = Path.GetExtension(entry.FullName);
                    if (ext.Equals(".pak", StringComparison.OrdinalIgnoreCase) ||
                        ext.Equals(".utoc", StringComparison.OrdinalIgnoreCase) ||
                        ext.Equals(".ucas", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch { }
            return false;
        }

        public void RefreshCodeBtn() { }
        public void OpenOfficialSite()
        {
            ExternalLauncher.OpenWebsite(GamePaths.ModDownloadUrl);
        }
    }
}
