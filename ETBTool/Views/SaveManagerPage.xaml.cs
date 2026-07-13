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
    public class SaveItem
    {
        public string Difficulty { get; set; } = "";
        public string DiffColor { get; set; } = "#888888";
        public string SaveName { get; set; } = "";
        public string Size { get; set; } = "";
        public string Modified { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

    public partial class SaveManagerPage : Page
    {
        private SaveItem[] _items = Array.Empty<SaveItem>();
        private static readonly string[] SaveExts = { ".sav", ".sav1", ".sav2", ".sav3" };

        private (string tag, string cn, string en, string color)[] Diffs => new[]
        {
            ("Easy","简单",LanguageManager.DiffEasy,"#2ECC71"), ("Normal","普通",LanguageManager.DiffNormal,"#3498DB"),
            ("Hard","困难",LanguageManager.DiffHard,"#E67E22"), ("Nightmare","噩梦",LanguageManager.DiffNight,"#E74C3C"),
            ("Practice","练习",LanguageManager.DiffPractice,"#9B59B6"), ("Tutorial","教程",LanguageManager.DiffTutorial,"#1ABC9C"),
        };

        public SaveManagerPage() { InitializeComponent(); LanguageManager.LanguageChanged += RefreshText; SavePathBox.Text = GamePaths.SavePath; LoadFiles(); RefreshText(); }

        private void RefreshText()
        {
            PageTitle.Text = LanguageManager.SaveTitle; PageSub.Text = LanguageManager.SaveSub;
            LblPath.Text = LanguageManager.SavePath; LblList.Text = LanguageManager.SaveList;
            BtnOpen.Content = LanguageManager.BtnOpen; BtnCopy.Content = LanguageManager.BtnCopy;
            HDiff.Text = LanguageManager.ColDiff; HName.Text = LanguageManager.ColSaveName;
            HSize.Text = LanguageManager.ColSize; HMod.Text = LanguageManager.ColModified;
            BtnRefresh.Content = LanguageManager.BtnRefresh; BtnEdit.Content = LanguageManager.BtnEdit;
            BtnDel.Content = LanguageManager.BtnDelete; BtnClear.Content = LanguageManager.BtnClearAll;
        }

        private (string name, string diff, string color) ParseSaveFile(string fileName)
        {
            string raw = Path.GetFileNameWithoutExtension(fileName);
            if (!raw.StartsWith("MULTIPLAYER_", StringComparison.OrdinalIgnoreCase)) return ("", "", "");
            foreach (var (tag, cn, en, color) in Diffs)
            {
                var suffix = "_" + tag;
                if (raw.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var saveName = raw.Substring("MULTIPLAYER_".Length, raw.Length - "MULTIPLAYER_".Length - suffix.Length);
                    return (saveName, LanguageManager.IsChinese ? cn : en, color);
                }
            }
            var name = raw.Substring("MULTIPLAYER_".Length);
            return (name, "?", "#888888");
        }

        private void LoadFiles()
        {
            FileListBox.Items.Clear();
            try
            {
                if (!Directory.Exists(GamePaths.SavePath)) { _items = Array.Empty<SaveItem>(); return; }
                _items = Directory.GetFiles(GamePaths.SavePath)
                    .Where(f => SaveExts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Select(f => {
                        var fi = new FileInfo(f); var (name, diff, color) = ParseSaveFile(fi.Name); if (string.IsNullOrEmpty(name)) return null;
                        return new SaveItem { Difficulty = diff, DiffColor = color, SaveName = name, Size = $"{fi.Length / 1024.0:F1} KB", Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"), FilePath = f };
                    })
                    .Where(x => x != null).Cast<SaveItem>().OrderByDescending(x => x.Modified).ToArray();
                foreach (var item in _items) FileListBox.Items.Add(item);
                Logger.Log(LanguageManager.MsgSaveLoaded(_items.Length));
            }
            catch (Exception ex) { Logger.Log(ex.Message); }
        }

        private SaveItem[] GetSelectedItems() { var r = new List<SaveItem>(); foreach (var o in FileListBox.SelectedItems) if (o is SaveItem si) r.Add(si); return r.ToArray(); }

        private void OpenFolder(object s, RoutedEventArgs e) { try { if (!Directory.Exists(GamePaths.SavePath)) Directory.CreateDirectory(GamePaths.SavePath); Process.Start("explorer.exe", GamePaths.SavePath); } catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); } }
        private void CopyPath(object s, RoutedEventArgs e) { bool ok = Logger.SafeSetClipboard(GamePaths.SavePath); Toast.Show(ok ? LanguageManager.MsgCopied : LanguageManager.MsgCopyFail, ok ? ToastType.Success : ToastType.Error); }
        private void Refresh(object s, RoutedEventArgs e) { LoadFiles(); Toast.Show(LanguageManager.MsgRefreshed, ToastType.Info); }
        private void Edit(object s, RoutedEventArgs e) { var sel = GetSelectedItems(); if (sel.Length == 0) return; try { Process.Start(new ProcessStartInfo(sel[0].FilePath) { UseShellExecute = true }); } catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); } }

        private void Delete(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedItems(); if (sel.Length == 0) return;
            if (ThemedDialog.Show(Window.GetWindow(this), LanguageManager.MsgConfirmDelete(sel.Length), LanguageManager.BtnConfirm, MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try { foreach (var item in sel) File.Delete(item.FilePath); LoadFiles(); Toast.Show(LanguageManager.MsgSaveDeleted(sel.Length), ToastType.Success); } catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void ClearAll(object s, RoutedEventArgs e)
        {
            if (ThemedDialog.Show(Window.GetWindow(this), LanguageManager.MsgConfirmClear, LanguageManager.BtnConfirm, MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try { foreach (var f in Directory.GetFiles(GamePaths.SavePath).Where(f => SaveExts.Contains(Path.GetExtension(f).ToLowerInvariant()))) File.Delete(f); LoadFiles(); Toast.Show(LanguageManager.MsgCleared, ToastType.Success); } catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}