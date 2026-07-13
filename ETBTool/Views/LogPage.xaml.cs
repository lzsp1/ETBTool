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

        // ★ 语言切换时重新加载并翻译
        private void OnLangChanged() => Dispatcher.Invoke(LoadLogs);

        // ★ 加载时翻译
        private void LoadLogs()
        {
            var lines = Logger.ReadAll().Select(l => LanguageManager.TranslateLog(l));
            LogTextBox.Text = string.Join(Environment.NewLine, lines);
            Dispatcher.BeginInvoke(() => LogTextBox.ScrollToEnd(), DispatcherPriority.Loaded);
        }

        // ★ 新日志也翻译
        private void OnNew(string entry) =>
            Dispatcher.Invoke(() =>
            {
                LogTextBox.AppendText(Environment.NewLine + LanguageManager.TranslateLog(entry));
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