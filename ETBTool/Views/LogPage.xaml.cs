using System;
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
            LoadLogs();
            Logger.OnLogAdded += OnNew;
            Unloaded += (_, _) => Logger.OnLogAdded -= OnNew;
        }

        private void LoadLogs()
        {
            LogTextBox.Text = string.Join(Environment.NewLine, Logger.ReadAll());
            Dispatcher.BeginInvoke(() => LogTextBox.ScrollToEnd(), DispatcherPriority.Loaded);
        }

        private void OnNew(string entry) =>
            Dispatcher.Invoke(() => { LogTextBox.AppendText(Environment.NewLine + entry); LogTextBox.ScrollToEnd(); });

        private void Refresh_Click(object s, RoutedEventArgs e) => LoadLogs();

        private void Clear_Click(object s, RoutedEventArgs e)
        {
            if (ThemedDialog.Show(Window.GetWindow(this), "\u786E\u5B9A\u6E05\u7A7A\u65E5\u5FD7\uFF1F", "\u786E\u8BA4", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            { Logger.Clear(); LogTextBox.Clear(); Toast.Show("\u2713 \u5DF2\u6E05\u7A7A", ToastType.Success); }
        }
    }
}