using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ETBTool.Models;
using ETBTool.Utils;

namespace ETBTool.Views
{
    public partial class UpdatePage : Page
    {
        private UpdateResult? _latestUpdate;
        private bool _busy;

        public UpdatePage()
        {
            InitializeComponent();
            Loaded += (_, _) => { LanguageManager.LanguageChanged -= RefreshText; LanguageManager.LanguageChanged += RefreshText; RefreshText(); };
            Unloaded += (_, _) => LanguageManager.LanguageChanged -= RefreshText;
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
            BtnChangelog.Content = LanguageManager.IsChinese ? "查看更新记录" : "Release history";
            BtnChangelog.ToolTip = GamePaths.ReleaseHistoryUrl;
            DLBtn.Content = LanguageManager.IsChinese ? "下载并安装" : "Download and install";
        }

        private async void Check_Click(object s, RoutedEventArgs e)
        {
            if (_busy || Window.GetWindow(this) is not MainWindow owner) return;
            _busy = true;
            BtnCheck.IsEnabled = false;
            DLBtn.IsEnabled = false;
            StatusText.Text = LanguageManager.UpdateChecking;
            Progress.Visibility = Visibility.Visible;
            Progress.IsIndeterminate = true;
            UpdatePanel.Visibility = Visibility.Collapsed;
            _latestUpdate = null;
            try
            {
                var result = await owner.CheckForUpdatesAsync(false);
                if (!IsLoaded) return;
                StatusText.Text = result.message;
                if (!result.Succeeded)
                {
                    Toast.Show(result.message, ToastType.Error);
                    return;
                }
                if (result.hasUpdate)
                {
                    _latestUpdate = result;
                    UpdatePanel.Visibility = Visibility.Visible;
                    UpdateText.Text =
                        $"{LanguageManager.UpdateLatest}: v{result.latest}\n" +
                        $"{LanguageManager.UpdateCurrent}: v{GamePaths.DisplayVersion}\n\n" +
                        (string.IsNullOrWhiteSpace(result.changelog) ? LanguageManager.UpdateHint : result.changelog);
                    UpdatePanel.BeginAnimation(UIElement.OpacityProperty,
                        new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));
                    Toast.Show(LanguageManager.UpdateFound(result.latest), ToastType.Info);
                }
                else Toast.Show(LanguageManager.MsgNoUpdate, ToastType.Success);
            }
            catch (Exception ex) { StatusText.Text = ex.Message; Toast.Show(ex.Message, ToastType.Error); }
            finally
            {
                _busy = false;
                BtnCheck.IsEnabled = true;
                DLBtn.IsEnabled = true;
                Progress.IsIndeterminate = false;
                Progress.Visibility = Visibility.Collapsed;
            }
        }

        private async void DL_Click(object s, RoutedEventArgs e)
        {
            if (_busy || _latestUpdate == null || Window.GetWindow(this) is not MainWindow owner) return;
            _busy = true;
            BtnCheck.IsEnabled = false;
            DLBtn.IsEnabled = false;
            try
            {
                // Refresh metadata before downloading: signed URLs expire and mandatory policy can change.
                var result = await owner.CheckForUpdatesAsync(true);
                if (!IsLoaded) return;
                StatusText.Text = result.message;
                if (!result.Succeeded) Toast.Show(result.message, ToastType.Error);
                if (!result.Succeeded || !result.hasUpdate)
                {
                    _latestUpdate = null;
                    UpdatePanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
            finally { _busy = false; BtnCheck.IsEnabled = true; DLBtn.IsEnabled = true; }
        }

        private void OpenLog_Click(object s, RoutedEventArgs e)
        {
            ExternalLauncher.OpenWebsite(GamePaths.ReleaseHistoryUrl);
        }
    }
}
