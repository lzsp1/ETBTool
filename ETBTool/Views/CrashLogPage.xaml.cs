using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ETBTool.Models;
using ETBTool.Utils;
using ETBTool.Windows;

namespace ETBTool.Views
{
    public class CrashItem
    {
        public string Name { get; set; } = "";
        public string Size { get; set; } = "";
        public string Modified { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public partial class CrashLogPage : Page
    {
        private CrashItem[] _items = Array.Empty<CrashItem>();

        public CrashLogPage()
        {
            InitializeComponent();
            LanguageManager.LanguageChanged += RefreshText;
            LogPathBox.Text = GamePaths.CrashLogPath;
            LoadFiles();
            RefreshText();
        }

        private void RefreshText()
        {
            PageTitle.Text = LanguageManager.CrashTitle;
            PageSub.Text = LanguageManager.CrashSub;
            LblPath.Text = LanguageManager.CrashPath;
            LblList.Text = LanguageManager.CrashList;
            BtnOpen.Content = LanguageManager.BtnOpen;
            BtnCopy.Content = LanguageManager.BtnCopy;
            HName.Text = LanguageManager.ColName;
            HSize.Text = LanguageManager.ColSize;
            HMod.Text = LanguageManager.ColModified;
            BtnRefresh.Content = LanguageManager.BtnRefresh;
            BtnOpenSel.Content = LanguageManager.BtnOpenSel;
            BtnDel.Content = LanguageManager.BtnDelete;
            BtnClear.Content = LanguageManager.BtnClearLogs;
        }

        private void LoadFiles()
        {
            FileListBox.Items.Clear();
            try
            {
                if (!Directory.Exists(GamePaths.CrashLogPath))
                {
                    _items = Array.Empty<CrashItem>();
                    return;
                }
                _items = Directory.GetFiles(GamePaths.CrashLogPath)
                    .Select(f =>
                    {
                        var fi = new FileInfo(f);
                        return new CrashItem
                        {
                            Name = fi.Name,
                            Size = $"{fi.Length / 1024.0:F1} KB",
                            Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                            FilePath = f
                        };
                    })
                    .OrderByDescending(x => x.Modified)
                    .ToArray();

                foreach (var item in _items) FileListBox.Items.Add(item);
                Logger.Log(LanguageManager.MsgLogLoaded(_items.Length));
            }
            catch (Exception ex) { Logger.Log(ex.Message); }
        }

        private CrashItem[] GetSelectedItems()
        {
            var r = new List<CrashItem>();
            foreach (var o in FileListBox.SelectedItems)
                if (o is CrashItem ci) r.Add(ci);
            return r.ToArray();
        }

        private void OpenFolder_Click(object s, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(GamePaths.CrashLogPath))
                    Directory.CreateDirectory(GamePaths.CrashLogPath);
                Process.Start("explorer.exe", GamePaths.CrashLogPath);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void CopyPath_Click(object s, RoutedEventArgs e)
        {
            bool ok = Logger.SafeSetClipboard(GamePaths.CrashLogPath);
            Toast.Show(ok ? LanguageManager.MsgCopied : LanguageManager.MsgCopyFail,
                ok ? ToastType.Success : ToastType.Error);
        }

        private void Refresh_Click(object s, RoutedEventArgs e)
        {
            LoadFiles();
            Toast.Show(LanguageManager.MsgRefreshed, ToastType.Info);
        }

        private void Open_Click(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedItems();
            if (sel.Length == 0) return;
            try
            {
                foreach (var item in sel)
                    Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void Delete_Click(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedItems();
            if (sel.Length == 0) return;
            if (ThemedDialog.Show(Window.GetWindow(this),
                LanguageManager.MsgConfirmDelLog(sel.Length),
                LanguageManager.BtnConfirm,
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                foreach (var item in sel) File.Delete(item.FilePath);
                LoadFiles();
                Toast.Show(LanguageManager.MsgSaveDeleted(sel.Length), ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        // ★ 改用 MsgCrashLogCleared
        private void ClearAll_Click(object s, RoutedEventArgs e)
        {
            if (ThemedDialog.Show(Window.GetWindow(this),
                LanguageManager.MsgConfirmClearLogs,
                LanguageManager.BtnConfirm,
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                foreach (var f in Directory.GetFiles(GamePaths.CrashLogPath))
                    File.Delete(f);
                LoadFiles();
                Toast.Show(LanguageManager.MsgCrashLogCleared, ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}