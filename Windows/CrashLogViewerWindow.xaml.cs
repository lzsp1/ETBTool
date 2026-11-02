using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ETBTool.Utils;
using Microsoft.Win32;

namespace ETBTool.Windows
{
    public partial class CrashLogViewerWindow : Window
    {
        private readonly string _filePath;
        private readonly string _content;

        public CrashLogViewerWindow(string filePath)
        {
            InitializeComponent();

            _filePath = filePath;
            var fileName = Path.GetFileName(filePath);
            TitleText.Text = fileName;
            Title = $"查看 - {fileName}";

            try
            {
                _content = TextFileReader.ReadAllText(filePath);
                ContentBox.Text = _content;
                ContentBox.ScrollToHome();
            }
            catch (Exception ex)
            {
                _content = "";
                ContentBox.Text = LanguageManager.IsChinese
                    ? $"读取失败: {ex.Message}"
                    : $"Read failed: {ex.Message}";
            }
        }

        private void Copy_Click(object s, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_content)) return;
            bool ok = Logger.SafeSetClipboard(_content);
            Toast.Show(
                ok ? (LanguageManager.IsChinese ? "已复制到剪贴板" : "Copied to clipboard")
                   : (LanguageManager.IsChinese ? "复制失败" : "Copy failed"),
                ok ? ToastType.Success : ToastType.Error);
        }

        private void Export_Click(object s, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = LanguageManager.IsChinese ? "导出主要日志" : "Export Main Log",
                FileName = Path.GetFileName(_filePath),
                Filter = "XML|*.xml|All|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.Copy(_filePath, dlg.FileName, true);
                    Toast.Show(LanguageManager.IsChinese ? "导出成功" : "Export successful", ToastType.Success);
                }
                catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
            }
        }

        private void TitleBar_Drag(object s, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) DragMove();
        }

        private void Close_Click(object s, RoutedEventArgs e) => Close();
    }
}