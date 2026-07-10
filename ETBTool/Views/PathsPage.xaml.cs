using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ETBTool.Models;
using ETBTool.Utils;

namespace ETBTool.Views
{
    public partial class PathsPage : Page
    {
        public PathsPage()
        {
            InitializeComponent();
            Path1.Text = GamePaths.SavePath;
            Path2.Text = GamePaths.CrashLogPath;
            Path3.Text = GamePaths.GameInstallPath;
            Path4.Text = GamePaths.ModPath;
        }

        private string Resolve(string t) => t switch
        {
            "save" => GamePaths.SavePath,
            "crash" => GamePaths.CrashLogPath,
            "ue4" => GamePaths.GameInstallPath,
            "mod" => GamePaths.ModPath,
            _ => ""
        };

        private void Copy_Click(object s, RoutedEventArgs e)
        {
            if (s is Button b && b.Tag is string t)
            {
                var p = Resolve(t);
                if (!string.IsNullOrEmpty(p))
                {
                    bool ok = Logger.SafeSetClipboard(p);
                    Toast.Show(ok ? "\u2713 \u5DF2\u590D\u5236" : "\u2717 \u5931\u8D25",
                        ok ? ToastType.Success : ToastType.Error);
                }
            }
        }

        private void Open_Click(object s, RoutedEventArgs e)
        {
            if (s is Button b && b.Tag is string t)
            {
                var p = Resolve(t);
                try
                {
                    if (!string.IsNullOrEmpty(p))
                    {
                        if (!Directory.Exists(p)) Directory.CreateDirectory(p);
                        Process.Start("explorer.exe", p);
                    }
                }
                catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
            }
        }
    }
}