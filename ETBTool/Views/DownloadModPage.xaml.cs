using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using ETBTool.Models;
using ETBTool.Utils;

namespace ETBTool.Views
{
    public partial class DownloadModPage : Page
    {
        public DownloadModPage() { InitializeComponent(); }

        private void Go_Click(object s, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(GamePaths.ModDownloadUrl) { UseShellExecute = true });
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}