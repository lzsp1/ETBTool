using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using ETBTool.Models;
using ETBTool.Utils;
using ETBTool.Windows;

namespace ETBTool.Views
{
    public partial class DownloadModPage : Page
    {
        public DownloadModPage() { InitializeComponent(); LanguageManager.LanguageChanged += RefreshText; RefreshText(); }

        private void RefreshText()
        {
            PageTitle.Text = LanguageManager.DlTitle;
            PageSub.Text = LanguageManager.DlSub;
            BtnGo.Content = LanguageManager.DlGo;
        }

        private void Go_Click(object s, RoutedEventArgs e)
        {
            try
            {
                if (!LanguageManager.IsChinese)
                {
                    // ★ 英文模式：三个按钮 Yes/No/Cancel
                    var answer = ThemedDialog.Show(
                        Window.GetWindow(this),
                        LanguageManager.DlLangDetect,
                        LanguageManager.DlLangTitle,
                        MessageBoxButton.YesNoCancel);

                    if (answer == MessageBoxResult.Yes)
                        Process.Start(new ProcessStartInfo(GamePaths.ModDownloadUrl) { UseShellExecute = true });
                    else if (answer == MessageBoxResult.No)
                        Process.Start(new ProcessStartInfo("https://www.nexusmods.com/games/escapethebackrooms/mods?page=1") { UseShellExecute = true });
                    // Cancel = 不操作
                    return;
                }
                Process.Start(new ProcessStartInfo(GamePaths.ModDownloadUrl) { UseShellExecute = true });
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}