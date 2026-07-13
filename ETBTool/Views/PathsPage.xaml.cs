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
            LanguageManager.LanguageChanged += RefreshText;
            Path1.Text = GamePaths.SavePath;
            Path2.Text = GamePaths.CrashLogPath;
            Path3.Text = GamePaths.GameInstallPath;
            Path4.Text = GamePaths.ModPath;
            RefreshText();
        }

        private void RefreshText()
        {
            PageTitle.Text = LanguageManager.PathsTitle;
            PageSub.Text = LanguageManager.PathsSub;
            Lbl1.Text = LanguageManager.PathsSave;
            Lbl2.Text = LanguageManager.PathsCrash;
            Lbl3.Text = LanguageManager.PathsGame;
            Lbl4.Text = LanguageManager.PathsMod;

            Copy1.Content = LanguageManager.BtnCopy;
            Copy2.Content = LanguageManager.BtnCopy;
            Copy3.Content = LanguageManager.BtnCopy;
            Copy4.Content = LanguageManager.BtnCopy;
            Open1.Content = LanguageManager.BtnOpen;
            Open2.Content = LanguageManager.BtnOpen;
            Open3.Content = LanguageManager.BtnOpen;
            Open4.Content = LanguageManager.BtnOpen;
        }

        private void Copy_Click(object s, RoutedEventArgs e)
        {
            string path = "";
            if (s == Copy1) path = Path1.Text;
            else if (s == Copy2) path = Path2.Text;
            else if (s == Copy3) path = Path3.Text;
            else if (s == Copy4) path = Path4.Text;
            if (string.IsNullOrEmpty(path)) return;
            bool ok = Logger.SafeSetClipboard(path);
            Toast.Show(ok ? LanguageManager.MsgCopied : LanguageManager.MsgCopyFail,
                ok ? ToastType.Success : ToastType.Error);
        }

        private void Open_Click(object s, RoutedEventArgs e)
        {
            string path = "";
            if (s == Open1) path = Path1.Text;
            else if (s == Open2) path = Path2.Text;
            else if (s == Open3) path = Path3.Text;
            else if (s == Open4) path = Path4.Text;
            try
            {
                if (!string.IsNullOrEmpty(path))
                {
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    Process.Start("explorer.exe", path);
                }
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}