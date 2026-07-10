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

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _initDone = true;
            NavSave.IsChecked = true;

            Title = $"ETB Tool v{GamePaths.DisplayVersion}";
            TitleVersionText.Text = $"v{GamePaths.DisplayVersion}";

            Logger.Log("应用程序已启动");
            Toast.Show("已启动", ToastType.Success);

            await CheckUpdateOnStart();
        }

        private async Task CheckUpdateOnStart()
        {
            var result = await UpdateChecker.CheckAsync();
            Logger.Log($"自动更新检查: {result.message}");

            if (!result.hasUpdate) return;

            var answer = ThemedDialog.Show(this,
                $"{result.message}\n\n是否前往下载页面？",
                "发现新版本", MessageBoxButton.YesNo);

            if (answer != MessageBoxResult.Yes) return;
            UpdateChecker.OpenUpdatePage(result.downloadUrl, result.mirrorUrl);
        }

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (!_initDone) return;
            if (sender is not RadioButton rb) return;
            if (rb.IsChecked != true) return;

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

            if (page != null)
            {
                ContentFrame.Navigate(page);
                AnimateIn();
            }
        }

        private void ToggleNav(object sender, RoutedEventArgs e)
        {
            double target = _navOpen ? 56 : 220;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            Sidebar.CacheMode = new BitmapCache();
            Sidebar.BeginAnimation(WidthProperty,
                new DoubleAnimation(target, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease });

            double opacity = _navOpen ? 0 : 1;
            var fadeDur = TimeSpan.FromMilliseconds(120);
            var fadeDelay = _navOpen ? TimeSpan.Zero : TimeSpan.FromMilliseconds(60);
            var anim = new DoubleAnimation(opacity, fadeDur) { BeginTime = fadeDelay, EasingFunction = ease };
            NavTop.BeginAnimation(OpacityProperty, anim);
            NavBot.BeginAnimation(OpacityProperty, anim);
            _navOpen = !_navOpen;
        }

        private void AnimateIn()
        {
            ContentFrame.Opacity = 0;
            var tt = ContentFrame.RenderTransform as TranslateTransform;
            if (tt != null) tt.Y = 12;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            ContentFrame.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250)) { EasingFunction = ease });
            if (tt != null)
                tt.BeginAnimation(TranslateTransform.YProperty,
                    new DoubleAnimation(12, 0, TimeSpan.FromMilliseconds(250)) { EasingFunction = ease });
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) Max_Click(sender, e);
            else DragMove();
        }

        private void Min_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState.Minimized;

        private void Max_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal : WindowState.Maximized;

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}