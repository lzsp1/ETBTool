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
        private string _latestDownloadUrl = "", _latestMirrorUrl = "";

        public UpdatePage()
        {
            InitializeComponent();
            LanguageManager.LanguageChanged += RefreshText;
            Unloaded += (_, _) => LanguageManager.LanguageChanged -= RefreshText;
            CurVer.Text = $"v{GamePaths.DisplayVersion}";
            RefreshText();
        }

        private void RefreshText()
        {
            PageTitle.Text = LanguageManager.UpdateTitle;
            LblCurVer.Text = LanguageManager.UpdateCurVer;
            CurVer.Text = $"v{GamePaths.DisplayVersion}";
            LblCheck.Text = LanguageManager.UpdateTitle;
            BtnCheck.Content = LanguageManager.BtnCheckUpdate;
            LblLog.Text = LanguageManager.UpdateLogSection;
            BtnChangelog.Content = LanguageManager.BtnViewLog;
            DLBtn.Content = LanguageManager.BtnDL;
        }

        private async void Check_Click(object s, RoutedEventArgs e)
        {
            StatusText.Text = LanguageManager.UpdateChecking;
            Progress.Visibility = Visibility.Visible;
            Progress.IsIndeterminate = true;
            UpdatePanel.Visibility = Visibility.Collapsed;
            DLBtn.Visibility = Visibility.Collapsed;

            var result = await UpdateChecker.CheckAsync();

            Progress.IsIndeterminate = false;
            Progress.Value = 100;
            StatusText.Text = result.message;
            Logger.Log($"更新检查: {result.message}");

            if (result.hasUpdate)
            {
                _latestDownloadUrl = result.downloadUrl;
                _latestMirrorUrl = result.mirrorUrl;
                UpdatePanel.Visibility = Visibility.Visible;
                DLBtn.Visibility = Visibility.Visible;
                UpdateText.Text =
                    $"{LanguageManager.UpdateLatest}: v{result.latest}\n" +
                    $"{LanguageManager.UpdateCurrent}: v{GamePaths.DisplayVersion}\n\n" +
                    LanguageManager.UpdateHint;
                UpdatePanel.Opacity = 0;
                UpdatePanel.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));
                Toast.Show(LanguageManager.UpdateFound(result.latest), ToastType.Info);
            }
            else
            {
                Toast.Show(LanguageManager.MsgNoUpdate, ToastType.Success);
            }
        }

        private void DL_Click(object s, RoutedEventArgs e) =>
            UpdateChecker.OpenUpdatePage(_latestDownloadUrl, _latestMirrorUrl);

        private void OpenLog_Click(object s, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(GamePaths.UpdateDownloadUrl) { UseShellExecute = true });
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}