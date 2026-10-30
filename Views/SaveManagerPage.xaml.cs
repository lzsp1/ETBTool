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

namespace ETBTool.Views
{
    public class SaveItem
    {
        public string Difficulty { get; set; } = "";
        public string DiffColor { get; set; } = "#888888";
        public string SaveName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Size { get; set; } = "";
        public string Modified { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string SlotKey { get; set; } = "";  // MAINSAVE 中的槽位键
    }

    public partial class SaveManagerPage : Page
    {
        private SaveItem[] _items = Array.Empty<SaveItem>();
        private UE4SaveReader? _mainSave;
        private static readonly string[] SaveExts = { ".sav", ".sav1", ".sav2", ".sav3" };
        private const string MainSaveName = "MAINSAVE.sav";

        private (string tag, string cn, string en, string color)[] Diffs => new[]
        {
            ("Easy","简单",LanguageManager.DiffEasy,"#2ECC71"),
            ("Normal","普通",LanguageManager.DiffNormal,"#3498DB"),
            ("Hard","困难",LanguageManager.DiffHard,"#E67E22"),
            ("Nightmare","噩梦",LanguageManager.DiffNight,"#E74C3C"),
            ("Practice","练习",LanguageManager.DiffPractice,"#9B59B6"),
            ("Tutorial","教程",LanguageManager.DiffTutorial,"#1ABC9C"),
        };

        public SaveManagerPage()
        {
            InitializeComponent();
            LanguageManager.LanguageChanged += RefreshText;
            SavePathBox.Text = GamePaths.SavePath;
            LoadFiles();
            RefreshText();
        }

        private void RefreshText()
        {
            PageTitle.Text = LanguageManager.SaveTitle;
            PageSub.Text = LanguageManager.SaveSub;
            LblPath.Text = LanguageManager.SavePath;
            LblList.Text = LanguageManager.SaveList;
            BtnOpen.Content = LanguageManager.BtnOpen;
            BtnCopy.Content = LanguageManager.BtnCopy;
            HDiff.Text = LanguageManager.ColDiff;
            HName.Text = LanguageManager.ColSaveName;
            HSize.Text = LanguageManager.ColSize;
            HMod.Text = LanguageManager.ColModified;
            BtnRefresh.Content = LanguageManager.BtnRefresh;
            BtnEdit.Content = LanguageManager.BtnEdit;
            BtnDel.Content = LanguageManager.BtnDelete;
            BtnClear.Content = LanguageManager.BtnClearAll;
        }

        /// <summary>
        /// 从文件名解析难度和存档名
        /// </summary>
        private (string name, string diff, string color) ParseSaveFile(string fileName)
        {
            string raw = Path.GetFileNameWithoutExtension(fileName);
            if (!raw.StartsWith("MULTIPLAYER_", StringComparison.OrdinalIgnoreCase))
                return ("", "", "");
            foreach (var (tag, cn, en, color) in Diffs)
            {
                var suffix = "_" + tag;
                if (raw.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var saveName = raw.Substring("MULTIPLAYER_".Length,
                        raw.Length - "MULTIPLAYER_".Length - suffix.Length);
                    return (saveName, LanguageManager.IsChinese ? cn : en, color);
                }
            }
            var name = raw.Substring("MULTIPLAYER_".Length);
            return (name, "?", "#888888");
        }

        private void LoadFiles()
        {
            FileListBox.Items.Clear();
            _items = Array.Empty<SaveItem>();
            _mainSave = null;
            EmptyState.Text = LanguageManager.IsChinese ? "没有存档文件" : "No save files";
            EmptyState.Visibility = Visibility.Visible;

            try
            {
                if (!Directory.Exists(GamePaths.SavePath))
                {
                    _items = Array.Empty<SaveItem>();
                    return;
                }

                // 读取 MAINSAVE.sav
                var mainSavePath = Path.Combine(GamePaths.SavePath, MainSaveName);
                if (File.Exists(mainSavePath))
                {
                    _mainSave = new UE4SaveReader();
                    if (!_mainSave.Load(mainSavePath))
                    {
                        Logger.Log("MAINSAVE.sav 解析失败，使用文件名模式显示");
                        _mainSave = null;
                    }
                    else
                    {
                        Logger.Log($"MAINSAVE.sav 已加载，{_mainSave.Entries.Count} 个条目");
                    }
                }

                // 扫描 .sav 文件
                var saveFiles = Directory.GetFiles(GamePaths.SavePath)
                    .Where(f => SaveExts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Where(f => !Path.GetFileName(f).Equals(MainSaveName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                var items = new List<SaveItem>();

                foreach (var f in saveFiles)
                {
                    var fi = new FileInfo(f);
                    var (parsedName, diff, color) = ParseSaveFile(fi.Name);

                    // 尝试从 MAINSAVE 获取显示名
                    string displayName = parsedName;
                    string slotKey = "";

                    if (_mainSave != null)
                    {
                        var entry = _mainSave.FindBySlotName(fi.Name);
                        if (entry != null)
                        {
                            if (!string.IsNullOrWhiteSpace(entry.DisplayName))
                                displayName = entry.DisplayName;
                            slotKey = entry.SlotFileName;
                        }
                    }

                    // Settings and Mod saves are not user save slots.
                    if (string.IsNullOrEmpty(parsedName)) continue;

                    items.Add(new SaveItem
                    {
                        Difficulty = diff,
                        DiffColor = color,
                        SaveName = parsedName,
                        DisplayName = displayName,
                        Size = $"{fi.Length / 1024.0:F1} KB",
                        Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                        FilePath = f,
                        SlotKey = slotKey
                    });
                }

                _items = items.OrderByDescending(x => x.Modified).ToArray();
                foreach (var item in _items) FileListBox.Items.Add(item);

                EmptyState.Visibility = _items.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
                Logger.Log(LanguageManager.MsgSaveLoaded(_items.Length));
            }
            catch (Exception ex)
            {
                EmptyState.Text = LanguageManager.IsChinese ? "无法读取存档目录，请检查路径与访问权限" : "Cannot read save folder. Check its path and permissions.";
                EmptyState.Visibility = Visibility.Visible;
                Logger.Log(ex.Message);
            }
        }

        private SaveItem[] GetSelectedItems()
        {
            var r = new List<SaveItem>();
            foreach (var o in FileListBox.SelectedItems)
                if (o is SaveItem si) r.Add(si);
            return r.ToArray();
        }

        private void OpenFolder(object s, RoutedEventArgs e)
        {
            ExternalLauncher.OpenFolder(GamePaths.SavePath);
        }

        private void CopyPath(object s, RoutedEventArgs e)
        {
            bool ok = Logger.SafeSetClipboard(GamePaths.SavePath);
            Toast.Show(ok ? LanguageManager.MsgCopied : LanguageManager.MsgCopyFail,
                ok ? ToastType.Success : ToastType.Error);
        }

        private void Refresh(object s, RoutedEventArgs e)
        {
            LoadFiles();
            Toast.Show(LanguageManager.MsgRefreshed, ToastType.Info);
        }

        private void Edit(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedItems();
            if (sel.Length == 0) return;
            ExternalLauncher.OpenFile(sel[0].FilePath);
        }

        private void Item_DblClick(object s, MouseButtonEventArgs e) => Edit(s, e);

        private void Delete(object s, RoutedEventArgs e)
        {
            var sel = GetSelectedItems();
            if (sel.Length == 0) return;
            if (ThemedDialog.Show(Window.GetWindow(this),
                LanguageManager.MsgConfirmDelete(sel.Length),
                LanguageManager.BtnConfirm, MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                // 删除物理文件
                foreach (var item in sel)
                {
                    if (!string.IsNullOrEmpty(item.FilePath) && File.Exists(item.FilePath))
                        File.Delete(item.FilePath);
                }

                // MAINSAVE contains unrelated progression and is intentionally read-only.

                LoadFiles();
                Toast.Show(LanguageManager.MsgSaveDeleted(sel.Length), ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }

        private void ClearAll(object s, RoutedEventArgs e)
        {
            if (ThemedDialog.Show(Window.GetWindow(this),
                LanguageManager.MsgConfirmClear, LanguageManager.BtnConfirm,
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            try
            {
                // 只删除 MULTIPLAYER_ 开头的存档文件
                foreach (var f in Directory.GetFiles(GamePaths.SavePath)
                    .Where(f => SaveExts.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Where(f => Path.GetFileNameWithoutExtension(f)
                        .StartsWith("MULTIPLAYER_", StringComparison.OrdinalIgnoreCase)))
                {
                    File.Delete(f);
                }


                LoadFiles();
                Toast.Show(LanguageManager.MsgCleared, ToastType.Success);
            }
            catch (Exception ex) { Toast.Show(ex.Message, ToastType.Error); }
        }
    }
}