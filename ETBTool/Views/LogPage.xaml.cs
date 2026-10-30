using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ETBTool.Utils;
using ETBTool.Windows;

namespace ETBTool.Views
{
    public partial class LogPage : Page
    {
        public LogPage()
        {
            InitializeComponent();
            LanguageManager.LanguageChanged += OnLangChanged;
            LoadLogs();
            Logger.OnLogAdded += OnNew;
            Unloaded += (_, _) => { Logger.OnLogAdded -= OnNew; LanguageManager.LanguageChanged -= OnLangChanged; };
            RefreshText();
        }

        private void RefreshText()
        {
            PageTitle.Text = LanguageManager.LogTitle;
            PageSub.Text = LanguageManager.LogSub;
            BtnRefresh.Content = LanguageManager.BtnRefresh;
            BtnClear.Content = LanguageManager.BtnClearLog;
        }

        private void OnLangChanged() => Dispatcher.Invoke(() =>
        {
            RefreshText();
            LoadLogs();
        });

        // 日志内容始终以中文显示，历史日志文件保持原样。
        private void LoadLogs()
        {
            var lines = Logger.ReadAll().Select(l => Logger.NormalizeForDisplay(l));
            LogTextBox.Text = string.Join(Environment.NewLine, lines);
            Dispatcher.BeginInvoke(() => LogTextBox.ScrollToEnd(), DispatcherPriority.Loaded);
        }

        // 新日志与历史日志使用同一套显示规则。
        private void OnNew(string entry) =>
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(Environment.NewLine + Logger.NormalizeForDisplay(entry));
                LogTextBox.ScrollToEnd();
            });

        private void Refresh_Click(object s, RoutedEventArgs e) => LoadLogs();

        private void Clear_Click(object s, RoutedEventArgs e)
        {
            if (ThemedDialog.Show(Window.GetWindow(this), LanguageManager.MsgConfirmClearLog, LanguageManager.BtnConfirm, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            { Logger.Clear(); LogTextBox.Clear(); Toast.Show(LanguageManager.MsgLogCleared, ToastType.Success); }
        }
    }
}