using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ETBTool.Models;
using ETBTool.Utils;
using ETBTool.Views;
using ETBTool.Windows;

namespace ETBTool
{
    public partial class MainWindow : Window
    {
        private bool _navOpen = true;
        private bool _initDone;

        public MainWindow() { InitializeComponent(); Loaded += OnLoaded; LanguageManager.LanguageChanged += RefreshNav; }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            LanguageManager.LoadLanguage();
            _initDone = true;
            NavSave.IsChecked = true;
            Title = $"ETB Tool v{GamePaths.DisplayVersion}";
            // ★ 只显示版本号
            TitleVersionText.Text = $"v{GamePaths.DisplayVersion}";
            RefreshNav();
            Logger.Log("应用已启动");
            Toast.Show(LanguageManager.MsgStarted, ToastType.Success);
            await CheckUpdateOnStart();
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

        private async Task CheckUpdateOnStart()
        {
            var r = await UpdateChecker.CheckAsync();
            Logger.Log($"自动更新检查: {r.message}");
            if (!r.hasUpdate) return;
            if (ThemedDialog.Show(this, $"{r.message}\n\n{(LanguageManager.IsChinese ? "是否前往下载页面？" : "Go to download page?")}",
                LanguageManager.IsChinese ? "发现新版本" : "Update", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                UpdateChecker.OpenUpdatePage(r.downloadUrl, r.mirrorUrl);
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
            double target = _navOpen ? 56 : 220;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            Sidebar.CacheMode = new BitmapCache();
            Sidebar.BeginAnimation(WidthProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease });
            var anim = new DoubleAnimation(_navOpen ? 0 : 1, TimeSpan.FromMilliseconds(120))
            { BeginTime = _navOpen ? TimeSpan.Zero : TimeSpan.FromMilliseconds(60), EasingFunction = ease };
            NavTop.BeginAnimation(OpacityProperty, anim); NavBot.BeginAnimation(OpacityProperty, anim);
            _navOpen = !_navOpen;
        }

        private void AnimateIn()
        {
            ContentFrame.Opacity = 0;
            var tt = ContentFrame.RenderTransform as TranslateTransform; if (tt != null) tt.Y = 12;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            ContentFrame.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)) { EasingFunction = ease });
            tt?.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(250)) { EasingFunction = ease });
        }

        private void TitleBar_MouseDown(object s, MouseButtonEventArgs e) { if (e.ClickCount == 2) Max_Click(s, e); else DragMove(); }
        private void Min_Click(object s, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Max_Click(object s, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object s, RoutedEventArgs e) => Close();
    }
}