using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ETBTool.Models;
using ETBTool.Utils;
using ETBTool.Windows;
using Microsoft.Win32;

namespace ETBTool.Views
{
    public class FileItem { public string Name { get; set; } = ""; public string Type { get; set; } = ""; public string Size { get; set; } = ""; public string Modified { get; set; } = ""; }

    public partial class UE4ManagerPage : Page
    {
        private string _rootDir = "", _currentDir = "";
        private string[] _dirs = Array.Empty<string>(), _files = Array.Empty<string>();

        public UE4ManagerPage() { InitializeComponent(); LanguageManager.LanguageChanged += RefreshText; _rootDir = GamePaths.UE4Path; PathBox.Text = _rootDir; NavigateTo(_rootDir); RefreshText(); }

        private void RefreshText()
        {
            PageTitle.Text = LanguageManager.UE4Title; PageSub.Text = LanguageManager.UE4Sub; LblPath.Text = LanguageManager.UE4GamePath;
            LblContents.Text = LanguageManager.UE4Contents; BtnOpen.Content = LanguageManager.BtnOpen; BtnCopy.Content = LanguageManager.BtnCopy;
            HName.Text = LanguageManager.ColName; HType.Text = LanguageManager.ColType; HSize.Text = LanguageManager.ColSize; HMod.Text = LanguageManager.ColModified;
            BtnUp.Content = LanguageManager.BtnUpDir; BtnRefresh.Content = LanguageManager.BtnRefresh;
            BtnImport.Content = LanguageManager.BtnImport; BtnDel.Content = LanguageManager.BtnDelete; BtnWipe.Content = LanguageManager.BtnWipeAll;
        }

        private void NavigateTo(string path) { _currentDir = path; CurrentDirText.Text = path; LoadItems(); }

        private void LoadItems()
        {
            ItemListBox.Items.Clear(); try
            {
                if (!Directory.Exists(_currentDir)) { _dirs = Array.Empty<string>(); _files = Array.Empty<string>(); return; }
                _dirs = Directory.GetDirectories(_currentDir).OrderBy(d => d).ToArray(); _files = Directory.GetFiles(_currentDir).OrderBy(f => f).ToArray();
                bool showUp = _currentDir != _rootDir && Directory.GetParent(_currentDir) != null;
                if (showUp) ItemListBox.Items.Add(new FileItem { Name = LanguageManager.MsgUpDir, Type = LanguageManager.ColFolder, Size = "-", Modified = "-" });
                foreach (var d in _dirs) { var i = new DirectoryInfo(d); ItemListBox.Items.Add(new FileItem { Name = i.Name, Type = LanguageManager.ColFolder, Size = "-", Modified = i.LastWriteTime.ToString("yyyy-MM-dd HH:mm") }); }
                foreach (var f in _files) { var i = new FileInfo(f); ItemListBox.Items.Add(new FileItem { Name = i.Name, Type = i.Extension.TrimStart('.').ToUpperInvariant(), Size = $"{i.Length / 1024.0:F1} KB", Modified = i.LastWriteTime.ToString("yyyy-MM-dd HH:mm") }); }
            }
            catch (Exception ex) { Logger.Log(ex.Message); }
        }

        private int GetOffset() => (_currentDir != _rootDir && Directory.GetParent(_currentDir) != null) ? 1 : 0;

        private void Item_DblClick(object s, MouseButtonEventArgs e)
        {
            var idx = ItemListBox.SelectedIndex; if (idx < 0) return; var off = GetOffset(); if (off == 1 && idx == 0) { GoUp(s, new RoutedEventArgs()); return; }
            var realIdx = idx - off; if (realIdx >= 0 && realIdx < _dirs.Length) NavigateTo(_dirs[realIdx]);
            else { var fi = realIdx - _dirs.Length; if (fi >= 0 && fi < _files.Length) try { Process.Start("explorer.exe", $"/select,\"{_files[fi]}\""); } catch { } }
        }

        private void GoUp(object s, RoutedEventArgs e) { var p = Directory.GetParent(_currentDir); if (p != null && _currentDir != _rootDir) NavigateTo(p.FullName); }

        private void OpenRoot(object s, RoutedEventArgs e)
        {
            var answer = ThemedDialog.Show(Window.GetWindow(this), LanguageManager.TipOpenPath, LanguageManager.IsChinese ? "提示" : "Tip", MessageBoxButton.OKCancel);
            if (answer != MessageBoxResult.OK) return; try
            {
                bool ok = Logger.SafeSetClipboard(_rootDir); Toast.Show(ok ? LanguageManager.MsgCopied : LanguageManager.MsgCopyFail, ok ? ToastType.Success : ToastType.Warning);
                Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.MyComputer));
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void CopyRoot(object s, RoutedEventArgs e) { bool ok = Logger.SafeSetClipboard(_rootDir); Toast.Show(ok ? LanguageManager.MsgCopied : LanguageManager.MsgCopyFail, ok ? ToastType.Success : ToastType.Error); }
        private void Refresh(object s, RoutedEventArgs e) { LoadItems(); Toast.Show(LanguageManager.MsgRefreshed, ToastType.Info); }
        private void Import(object s, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Title = LanguageManager.BtnImport, Multiselect = true, Filter = "All|*.*" }; if (dlg.ShowDialog() != true) return;
            try { foreach (var f in dlg.FileNames) File.Copy(f, Path.Combine(_currentDir, Path.GetFileName(f)), true); LoadItems(); Toast.Show(LanguageManager.MsgImportMod(dlg.FileNames.Length), ToastType.Success); } catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void DelSel(object s, RoutedEventArgs e)
        {
            var selected = new List<(string path, bool isDir)>(); foreach (var obj in ItemListBox.SelectedItems)
            {
                int idx = ItemListBox.Items.IndexOf(obj) - GetOffset();
                if (idx >= 0 && idx < _dirs.Length) selected.Add((_dirs[idx], true)); else if (idx >= _dirs.Length && idx - _dirs.Length < _files.Length) selected.Add((_files[idx - _dirs.Length], false));
            }
            if (selected.Count == 0) return; if (ThemedDialog.Show(Window.GetWindow(this), LanguageManager.MsgConfirmDelItems(selected.Count), LanguageManager.BtnConfirm, MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try { foreach (var (path, isDir) in selected) if (isDir) Directory.Delete(path, true); else File.Delete(path); LoadItems(); Toast.Show(LanguageManager.MsgSaveDeleted(selected.Count), ToastType.Success); } catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void DelAll(object s, RoutedEventArgs e)
        {
            if (ThemedDialog.Show(Window.GetWindow(this), LanguageManager.MsgWipeConfirm, LanguageManager.BtnConfirm, MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                foreach (var d in Directory.GetDirectories(_rootDir)) Directory.Delete(d, true); foreach (var f in Directory.GetFiles(_rootDir)) File.Delete(f);
                Process.Start(new ProcessStartInfo("steam://open/library") { UseShellExecute = true }); NavigateTo(_rootDir); Toast.Show(LanguageManager.MsgWiped, ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}