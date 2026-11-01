using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using ETBTool.Models;
using ETBTool.Services;
using ETBTool.Themes;
using ETBTool.Utils;
using ETBTool.Views;
using ETBTool.Windows;

namespace ETBTool
{
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty IsNavigationExpandedProperty =
            DependencyProperty.Register(nameof(IsNavigationExpanded), typeof(bool), typeof(MainWindow), new PropertyMetadata(true));
        public bool IsNavigationExpanded
        {
            get => (bool)GetValue(IsNavigationExpandedProperty);
            private set => SetValue(IsNavigationExpandedProperty, value);
        }
        private bool _initDone;
        private bool _usageNoticeShown;
        private bool _startupUpdateCheckStarted;
        private bool _updateInProgress;
        private bool _retryUpdates;
        private bool _closingForUpdate;
        private bool _windowAnimating;
        private Rect _normalBounds = Rect.Empty;
        private string _lastPromptedUpdate = "";
        private readonly CancellationTokenSource _updateLifetime = new();
        private readonly System.Windows.Threading.DispatcherTimer _updateRetryTimer = new();

        public MainWindow()
        {
            InitializeComponent();
            RootBorder.IsEnabled = false;
            _updateRetryTimer.Tick += RetryUpdateCheck;
            NetworkChange.NetworkAvailabilityChanged += UpdateNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += UpdateNetworkAddressChanged;
            Closed += CloseUpdateChecks;
            Loaded += OnLoaded;
            SizeChanged += Window_SizeChanged;
            LanguageManager.LanguageChanged += RefreshNav;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            LanguageManager.LoadLanguage();
            ThemeManager.Instance.LoadSavedTheme();

            _initDone = true;
            NavSettings.IsChecked = true;
            Title = $"ETB Tool v{GamePaths.DisplayVersion}";
            TitleVersionText.Text = $"v{GamePaths.DisplayVersion}";
            RefreshNav();
            Logger.Log("应用已启动");
            Toast.Show(LanguageManager.MsgStarted, ToastType.Success);

            TryOpenUsageNotice();

            if (!_startupUpdateCheckStarted)
            {
                _startupUpdateCheckStarted = true;
                await CheckUpdateOnStart();
            }
        }

        private sealed class NoticeState
        {
            public bool Opened { get; set; }
        }

        private void TryOpenUsageNotice()
        {
            if (_usageNoticeShown) return;
            _usageNoticeShown = true;
            try
            {
                var statePath = Path.Combine(GamePaths.AppConfigPath, "notice.json");
                if (ConfigPersistence.Read<NoticeState>(statePath).Opened) return;

                bool previouslyConfirmed = false;
                try
                {
                    if (File.Exists(GamePaths.SettingsFile))
                    {
                        using var document = JsonDocument.Parse(File.ReadAllText(GamePaths.SettingsFile));
                        previouslyConfirmed = document.RootElement.TryGetProperty("UsageNoticeConfirmed", out var value)
                            && value.ValueKind == JsonValueKind.True;
                    }
                }
                catch (Exception ex) { Logger.Log($"读取旧使用须知状态失败: {ex.Message}"); }

                if (!previouslyConfirmed)
                {
                    var txtPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "使用须知Usage Notice.txt");
                    if (!File.Exists(txtPath))
                    {
                        Logger.Log("首次运行，但未找到使用须知文件");
                        return;
                    }
                    using var process = Process.Start(new ProcessStartInfo(txtPath) { UseShellExecute = true });
                }

                ConfigPersistence.Write(statePath, new NoticeState { Opened = true });
                Logger.Log("使用须知首次打开状态已保存");
            }
            catch (Exception ex)
            {
                Logger.Log($"使用须知流程异常: {ex.Message}");
            }
        }

        private void RefreshNav()
        {
            NavSave.Content = LanguageManager.NavSave;
            NavCrash.Content = LanguageManager.NavCrash;
            NavUE4.Content = LanguageManager.NavUE4;
            NavMod.Content = LanguageManager.NavMod;
            NavDownload.Content = LanguageManager.NavDownload;
            NavPaths.Content = LanguageManager.NavPaths;
            NavLog.Content = LanguageManager.NavLog;
            NavSettings.Content = LanguageManager.NavSettings;
            NavAbout.Content = LanguageManager.NavAbout;
            NavUpdate.Content = LanguageManager.NavUpdate;
            double fs = LanguageManager.IsChinese ? 14 : 13;
            foreach (var rb in new[] { NavSave, NavCrash, NavUE4, NavMod, NavDownload, NavPaths, NavLog, NavSettings, NavAbout, NavUpdate })
                rb.FontSize = fs;
        }

        private Task<UpdateResult> CheckUpdateOnStart() => CheckForUpdatesAsync(true, initial: true);

        private void UpdateNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            if (e.IsAvailable) QueueUpdateRetry();
        }

        private void UpdateNetworkAddressChanged(object? sender, EventArgs e) => QueueUpdateRetry();

        private void QueueUpdateRetry()
        {
            if (_updateLifetime.IsCancellationRequested || Dispatcher.HasShutdownStarted) return;
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_updateLifetime.IsCancellationRequested) return;
                    _retryUpdates = true;
                    ScheduleUpdateRetry(TimeSpan.FromSeconds(3));
                }));
            }
            catch (InvalidOperationException) when (Dispatcher.HasShutdownStarted) { }
        }

        private void ScheduleUpdateRetry(TimeSpan delay)
        {
            if (_updateLifetime.IsCancellationRequested) return;
            _updateRetryTimer.Stop();
            _updateRetryTimer.Interval = delay;
            _updateRetryTimer.Start();
        }

        private async void RetryUpdateCheck(object? sender, EventArgs e)
        {
            _updateRetryTimer.Stop();
            if (!_retryUpdates || _updateLifetime.IsCancellationRequested) return;
            if (_updateInProgress || !_startupUpdateCheckStarted)
            {
                ScheduleUpdateRetry(TimeSpan.FromSeconds(3));
                return;
            }
            await CheckForUpdatesAsync(true, reconnect: true);
        }

        private void CloseUpdateChecks(object? sender, EventArgs e)
        {
            _closingForUpdate = true;
            _updateRetryTimer.Stop();
            _updateRetryTimer.Tick -= RetryUpdateCheck;
            NetworkChange.NetworkAvailabilityChanged -= UpdateNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged -= UpdateNetworkAddressChanged;
            _updateLifetime.Cancel();
        }

        internal async Task<UpdateResult> CheckForUpdatesAsync(bool offerInstall, bool initial = false, bool reconnect = false)
        {
            if (_updateInProgress || _updateLifetime.IsCancellationRequested)
                return new UpdateResult { message = LanguageManager.IsChinese ? "更新检查正在进行中。" : "An update check is already running." };
            _updateInProgress = true;
            RootBorder.IsEnabled = false;
            try
            {
                var result = await UpdateChecker.CheckAsync(_updateLifetime.Token);
                if (_updateLifetime.IsCancellationRequested) return result;
                Logger.Log($"更新检查: {result.message}");
                _retryUpdates = !result.Succeeded;
                if (!result.Succeeded)
                {
                    if (result.forceUpdate || UpdateChecker.HasMandatoryUpdate)
                    {
                        ThemedDialog.Show(this, "已知有必须安装的更新。当前无法连接更新服务，不能继续使用此版本。请恢复网络连接后重新启动工具。", "必须更新",
                            primaryLabel: "退出工具");
                        ExitForUpdate();
                    }
                    else if (initial)
                    {
                        ThemedDialog.Show(this, "未检测到有效的网络连接，暂时无法检查更新。联网后会自动检查并通知新版本；延迟更新可能影响使用体验。当前没有已知的强制更新，可以继续使用。", "暂时无法检查更新",
                            primaryLabel: "继续使用");
                    }
                    if (!_closingForUpdate) ScheduleUpdateRetry(TimeSpan.FromSeconds(30));
                    return result;
                }
                _updateRetryTimer.Stop();
                if (!result.hasUpdate) return result;
                result.forceUpdate |= UpdateChecker.HasMandatoryUpdate;
                if (result.forceUpdate || (offerInstall && (!reconnect || _lastPromptedUpdate != result.latest)))
                    await OfferUpdateAsync(result);
                return result;
            }
            catch (OperationCanceledException) when (_updateLifetime.IsCancellationRequested)
            {
                return new UpdateResult { message = "更新检查已取消。" };
            }
            catch (Exception ex)
            {
                Logger.Log($"更新流程失败: {ex.Message}");
                if (!_closingForUpdate)
                {
                    ThemedDialog.Show(this, ex.Message, LanguageManager.IsChinese ? "更新失败" : "Update failed");
                    if (UpdateChecker.HasMandatoryUpdate) ExitForUpdate();
                }
                return new UpdateResult { message = ex.Message };
            }
            finally
            {
                _updateInProgress = false;
                if (!_closingForUpdate) RootBorder.IsEnabled = !UpdateChecker.HasMandatoryUpdate;
            }
        }

        private void ExitForUpdate()
        {
            if (_closingForUpdate) return;
            _closingForUpdate = true;
            Application.Current.Shutdown();
        }

        private async Task OfferUpdateAsync(UpdateResult update)
        {
            _lastPromptedUpdate = update.latest;
            var log = string.IsNullOrWhiteSpace(update.changelog) ? "" : $"\n\n{(LanguageManager.IsChinese ? "更新日志：" : "Changelog:")}\n{update.changelog}";
            var prompt = update.message + log + "\n\n" + (update.forceUpdate
                ? (LanguageManager.IsChinese ? "这是强制更新，必须更新后才能继续使用。取消或关闭将退出工具。" : "This update is required. Canceling or closing will exit ETBTool.")
                : (LanguageManager.IsChinese ? "现在下载并安装更新吗？" : "Download and install this update now?"));
            var choice = ThemedDialog.Show(this, prompt, LanguageManager.IsChinese ? "发现新版本" : "Update", MessageBoxButton.YesNo,
                LanguageManager.IsChinese ? "下载并安装" : "Download and install",
                update.forceUpdate ? (LanguageManager.IsChinese ? "退出工具" : "Exit ETBTool") : (LanguageManager.IsChinese ? "稍后更新" : "Update later"));
            if (choice != MessageBoxResult.Yes)
            {
                if (update.forceUpdate) ExitForUpdate();
                return;
            }
            try
            {
                await DownloadUpdateAsync(update);
                ThemedDialog.Show(this,
                    LanguageManager.IsChinese
                        ? "更新文件已下载完成。\n\n程序即将关闭以完成安装，安装后会自动重新启动。\n请在此期间不要手动启动程序，以免更新失败。"
                        : "The update has been downloaded.\n\nETBTool will close to finish installing and start again automatically.\nPlease do not start it manually during this time.",
                    LanguageManager.IsChinese ? "即将关闭更新" : "Ready to restart",
                    MessageBoxButton.OK,
                    LanguageManager.IsChinese ? "开始更新" : "Continue");
                ExitForUpdate();
            }
            catch (OperationCanceledException)
            {
                if (update.forceUpdate && !_closingForUpdate) ExitForUpdate();
            }
            catch (Exception ex)
            {
                Logger.Log($"更新下载失败: {ex.Message}");
                if (!_closingForUpdate && UpdateChecker.LooksLikeRiskControl(ex))
                {
                    var risk = LanguageManager.IsChinese
                        ? "更新包下载失败，下载方式可能已被风控拦截（HTTP 404）。\n\n可在第二天重新下载，或现在通过直链下载：浏览器下载完成后，回到程序点击「选择文件」选择刚下载的 ZIP，程序会自动安装更新。"
                        : "The update download was blocked (HTTP 404).\n\nYou can retry tomorrow, or use the direct link now: after the browser finishes downloading, choose the ZIP with \"Select file\" to install automatically.";
                    var ask = ThemedDialog.Show(this, risk, LanguageManager.IsChinese ? "下载方式已被风控" : "Download blocked",
                        MessageBoxButton.YesNo,
                        LanguageManager.IsChinese ? "直链下载" : "Direct download",
                        LanguageManager.IsChinese ? "取消" : "Cancel");
                    if (ask == MessageBoxResult.Yes)
                    {
                        try
                        {
                            var directUrl = await UpdateChecker.GetDirectUpdateUrlAsync(_updateLifetime.Token);
                            if (string.IsNullOrWhiteSpace(directUrl)) throw new InvalidOperationException("服务器未返回直链地址");
                            if (!ExternalLauncher.OpenWebsite(directUrl)) throw new InvalidOperationException("无法打开浏览器");
                            var pick = ThemedDialog.Show(this,
                                LanguageManager.IsChinese
                                    ? "请等待浏览器下载完成，然后点击「选择文件」选择刚才下载的更新包，程序会自动完成安装。"
                                    : "Wait for the browser download to finish, then click \"Select file\" and choose the downloaded ZIP.",
                                LanguageManager.IsChinese ? "选择更新包" : "Select update package",
                                MessageBoxButton.OK,
                                LanguageManager.IsChinese ? "选择文件" : "Select file");
                            if (pick == MessageBoxResult.OK)
                            {
                                var dlg = new Microsoft.Win32.OpenFileDialog
                                {
                                    Title = LanguageManager.IsChinese ? "选择更新包" : "Select update package",
                                    Filter = "ZIP|*.zip"
                                };
                                if (dlg.ShowDialog() == true)
                                {
                                    await UpdateChecker.InstallFromPackageAsync(dlg.FileName, update, _updateLifetime.Token);
                                    ThemedDialog.Show(this,
                                        LanguageManager.IsChinese
                                            ? "更新已准备完成。程序即将关闭以完成安装，安装后会自动重新启动，请在此期间不要手动启动程序。"
                                            : "The update is ready. ETBTool will close to install and restart automatically. Do not start it manually meanwhile.",
                                        LanguageManager.IsChinese ? "即将关闭更新" : "Ready to restart",
                                        MessageBoxButton.OK,
                                        LanguageManager.IsChinese ? "开始更新" : "Continue");
                                    ExitForUpdate();
                                    return;
                                }
                            }
                        }
                        catch (Exception inner)
                        {
                            Logger.Log($"直链更新失败: {inner.Message}");
                            ThemedDialog.Show(this, inner.Message, LanguageManager.IsChinese ? "更新失败" : "Update failed");
                        }
                    }
                }
                else if (!_closingForUpdate)
                {
                    ThemedDialog.Show(this, ex.Message, LanguageManager.IsChinese ? "更新失败" : "Update failed",
                        primaryLabel: update.forceUpdate ? "退出工具" : "返回工具");
                }
                if (update.forceUpdate && !_closingForUpdate) ExitForUpdate();
            }
        }

        private async Task DownloadUpdateAsync(UpdateResult update)
        {
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(_updateLifetime.Token);
            cancel.CancelAfter(TimeSpan.FromMinutes(15));
            var finished = false;
            var dialog = new Window
            {
                Owner = this, Width = 460, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowStyle = WindowStyle.ToolWindow, ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false,
                Title = LanguageManager.IsChinese ? "正在更新" : "Updating"
            };
            var panel = new StackPanel { Margin = new Thickness(24) };
            var label = new TextBlock { Text = LanguageManager.IsChinese ? "正在下载更新..." : "Downloading update...", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };
            var bar = new ProgressBar { Height = 8, Minimum = 0, Maximum = 100 };
            var cancelButton = new Button
            {
                Content = update.forceUpdate ? (LanguageManager.IsChinese ? "取消并退出" : "Cancel and exit") : (LanguageManager.IsChinese ? "取消下载" : "Cancel download"),
                HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(0, 16, 0, 0)
            };
            cancelButton.Click += (_, _) => dialog.Close();
            dialog.Closing += (_, _) =>
            {
                if (finished) return;
                cancel.Cancel();
                if (update.forceUpdate && !_closingForUpdate) Dispatcher.BeginInvoke(new Action(ExitForUpdate));
            };
            panel.Children.Add(label); panel.Children.Add(bar); panel.Children.Add(cancelButton);
            dialog.Content = panel;
            dialog.Show();
            var progress = new Progress<DownloadProgress>(p =>
            {
                if (finished || cancel.IsCancellationRequested) return;
                bar.IsIndeterminate = p.Percent < 0;
                if (p.Percent >= 0) bar.Value = p.Percent;
                label.Text = p.StatusText;
            });
            try { await UpdateChecker.DownloadAndInstallAsync(update, progress, cancel.Token); }
            finally { finished = true; dialog.Close(); }
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initDone || sender is not RadioButton rb || rb.IsChecked != true) return;
            Page? page = rb.Name switch
            {
                "NavSave" => new SaveManagerPage(),
                "NavCrash" => new CrashLogPage(),
                "NavUE4" => new UE4ManagerPage(),
                "NavMod" => new ModManagerPage(),
                "NavDownload" => new DownloadModPage(),
                "NavPaths" => new PathsPage(),
                "NavLog" => new LogPage(),
                "NavAbout" => new AboutPage(),
                "NavSettings" => new SettingsPage(),
                "NavUpdate" => new UpdatePage(),
                _ => null
            };
            if (page != null) { ContentFrame.Navigate(page); AnimateIn(); }
        }

        private void ToggleNav(object sender, RoutedEventArgs e)
        {
            IsNavigationExpanded = !IsNavigationExpanded;
            double target = IsNavigationExpanded ? 220 : 64;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            Sidebar.BeginAnimation(WidthProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease });
        }

        private void AnimateIn()
        {
            ContentFrame.Opacity = 0;
            var tt = ContentFrame.RenderTransform as TranslateTransform;
            if (tt != null) tt.Y = 12;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            ContentFrame.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)) { EasingFunction = ease });
            tt?.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(250)) { EasingFunction = ease });
        }

        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) Max_Click(s, e); else DragMove();
        }

        private void Min_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Max_Click(object s, RoutedEventArgs e)
        {
            // 用窗口自身的几何动画（Left/Top/Width/Height）做缓慢放大/缩小，
            // 不再对整窗做 RenderTransform 缩放，避免大窗口重复合成导致掉帧。
            if (_windowAnimating) return;

            if (WindowState == WindowState.Maximized)
            {
                var target = _normalBounds.Width > 0 && _normalBounds.Height > 0
                    ? _normalBounds
                    : new Rect(Left, Top, Math.Max(Width, 900), Math.Max(Height, 640));
                var from = new Rect(Left, Top, ActualWidth > 0 ? ActualWidth : Width, ActualHeight > 0 ? ActualHeight : Height);
                _windowAnimating = true;
                WindowState = WindowState.Normal;
                Left = from.Left; Top = from.Top; Width = from.Width; Height = from.Height;
                AnimateWindowTo(target, () => SetMaxButtonIcon(false));
            }
            else
            {
                // 用窗口所在显示器的可用区域，而不是主显示器的工作区，
                // 否则多显示器时最大化会跳到主屏。
                _normalBounds = new Rect(Left, Top, ActualWidth > 0 ? ActualWidth : Width, ActualHeight > 0 ? ActualHeight : Height);
                var saved = _normalBounds;
                var area = GetMonitorWorkArea();
                AnimateWindowTo(new Rect(area.Left, area.Top, area.Width, area.Height), () =>
                {
                    WindowState = WindowState.Maximized;
                    SetMaxButtonIcon(true);
                    // 切换为最大化后，用保存的普通尺寸覆盖 SizeChanged 可能写入的值，
                    // 避免还原时窗口被拉长成整屏。
                    _normalBounds = saved;
                });
            }
        }

        private const uint MonitorDefaultToNearest = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Monitor;
            public NativeRect Work;
            public uint Flags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        /// <summary>返回窗口当前所在显示器的可用区域（已换算成 WPF 的 DIP 坐标）。</summary>
        private Rect GetMonitorWorkArea()
        {
            try
            {
                var handle = new WindowInteropHelper(this).Handle;
                var monitor = MonitorFromWindow(handle, MonitorDefaultToNearest);
                var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
                {
                    var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
                    var topLeft = transform.Transform(new Point(info.Work.Left, info.Work.Top));
                    var bottomRight = transform.Transform(new Point(info.Work.Right, info.Work.Bottom));
                    return new Rect(topLeft, bottomRight);
                }
            }
            catch { }
            return SystemParameters.WorkArea;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_windowAnimating || WindowState != WindowState.Normal) return;
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            _normalBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        }

        private void AnimateWindowTo(Rect target, Action completed)
        {
            _windowAnimating = true;
            var from = new Rect(Left, Top,
                ActualWidth > 0 ? ActualWidth : Width,
                ActualHeight > 0 ? ActualHeight : Height);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            const double durationMs = 320.0;
            EventHandler? tick = null;
            tick = (_, _) =>
            {
                var progress = Math.Min(1.0, watch.Elapsed.TotalMilliseconds / durationMs);
                // easeInOutCubic：起步和收尾都更柔和，中段不至于显得“弹”。
                var eased = progress < 0.5
                    ? 4 * progress * progress * progress
                    : 1 - Math.Pow(-2 * progress + 2, 3) / 2;
                Left = from.Left + (target.Left - from.Left) * eased;
                Top = from.Top + (target.Top - from.Top) * eased;
                Width = from.Width + (target.Width - from.Width) * eased;
                Height = from.Height + (target.Height - from.Height) * eased;
                if (progress < 1.0) return;
                CompositionTarget.Rendering -= tick;
                Left = target.Left; Top = target.Top; Width = target.Width; Height = target.Height;
                // 先执行回调再解除动画标记，避免回调里切换 WindowState 时
                // SizeChanged 把普通尺寸覆盖成整屏尺寸。
                completed();
                _windowAnimating = false;
            };
            CompositionTarget.Rendering += tick;
        }

        private void SetMaxButtonIcon(bool isMaximized)
        {
            var template = BtnMax.Template;
            if (template == null) return;

            BtnMax.ApplyTemplate();
            var icoMax = template.FindName("icoMax", BtnMax) as TextBlock;
            var icoRestore = template.FindName("icoRestore", BtnMax) as TextBlock;

            if (icoMax != null && icoRestore != null)
            {
                icoMax.Visibility = isMaximized ? Visibility.Collapsed : Visibility.Visible;
                icoRestore.Visibility = isMaximized ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void Close_Click(object s, RoutedEventArgs e) => Close();
    }
}
