using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ETBTool.Models;
using ETBTool.Utils;

namespace ETBTool.Views
{
    public partial class UpdatePage : Page
    {
        private string _latestDownloadUrl = "";
        private string _latestMirrorUrl = "";

        public UpdatePage()
        {
            InitializeComponent();
            CurVer.Text = $"v{GamePaths.DisplayVersion}";
        }

        private async void Check_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "正在检查更新...";
            Progress.Visibility = Visibility.Visible;
            Progress.IsIndeterminate = true;
            UpdatePanel.Visibility = Visibility.Collapsed;
            DLBtn.Visibility = Visibility.Collapsed;

            var result = await UpdateChecker.CheckAsync();

            Progress.IsIndeterminate = false;
            Progress.Value = 100;
            StatusText.Text = result.message;
            Logger.Log($"检查更新: {result.message}");

            if (result.hasUpdate)
            {
                _latestDownloadUrl = result.downloadUrl;
                _latestMirrorUrl = result.mirrorUrl;
                UpdatePanel.Visibility = Visibility.Visible;
                DLBtn.Visibility = Visibility.Visible;
                UpdateText.Text =
                    $"最新版本: v{result.latest}\n" +
                    $"当前版本: v{GamePaths.DisplayVersion}\n\n" +
                    $"请前往下载页面获取最新版本。";
                UpdatePanel.Opacity = 0;
                UpdatePanel.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));
                Toast.Show($"发现新版本 v{result.latest}", ToastType.Info);
            }
            else
            {
                Toast.Show("当前已是最新版本", ToastType.Success);
            }
        }

        private void DL_Click(object sender, RoutedEventArgs e)
        {
            UpdateChecker.OpenUpdatePage(_latestDownloadUrl, _latestMirrorUrl);
        }

        private void OpenLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(GamePaths.UpdateDownloadUrl) { UseShellExecute = true });
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}