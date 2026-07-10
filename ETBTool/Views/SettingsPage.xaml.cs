using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using ETBTool.Models;
using ETBTool.Themes;
using ETBTool.Utils;
using ETBTool.Windows;
using Button = System.Windows.Controls.Button;

namespace ETBTool.Views
{
    public partial class SettingsPage : Page
    {
        private static readonly string[] PresetColors =
        {
            "#6C5CE7","#7C4DFF","#FF6B6B","#00F5D4","#E74C3C","#3498DB",
            "#2ECC71","#F39C12","#9B59B6","#1ABC9C","#E67E22","#FF006E",
            "#00B894","#6AB04C","#FD79A8","#0984E3","#00CEC9","#FDCB6E"
        };

        public SettingsPage()
        {
            InitializeComponent();

            foreach (var n in ThemeManager.Instance.ThemeNames) StyleList.Items.Add(n);
            StyleList.SelectedIndex = (int)ThemeManager.Instance.CurrentStyle;

            foreach (var n in ThemeManager.Instance.ModeNames) ModeList.Items.Add(n);
            ModeList.SelectedIndex = (int)ThemeManager.Instance.CurrentMode;

            SavePathBox.Text = GamePaths.GetRawCustomSavePath() ?? "";
            CrashPathBox.Text = GamePaths.GetRawCustomCrashPath() ?? "";
            GamePathBox.Text = GamePaths.GetRawCustomGamePath() ?? "";
            BgPathText.Text = ThemeManager.Instance.BackgroundImagePath ?? "未设置";

            LoadSwatches();
        }

        private void LoadSwatches()
        {
            foreach (var c in PresetColors)
            {
                var swatch = new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(c)),
                    Margin = new Thickness(0, 0, 8, 8),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = c,
                    BorderThickness = new Thickness(2),
                    BorderBrush = new SolidColorBrush(Colors.Transparent)
                };
                swatch.MouseLeftButtonDown += (_, _) =>
                {
                    ThemeManager.Instance.SetCustomAccent(c);
                    Logger.Log($"  强调色改为: {c}");
                    Toast.Show("  强调色已更改", ToastType.Success);
                };
                ColorSwatches.Children.Add(swatch);
            }
        }

        private void Style_Changed(object s, SelectionChangedEventArgs e)
        {
            if (StyleList.SelectedIndex >= 0)
            {
                ThemeManager.Instance.CurrentStyle = (AppThemeStyle)StyleList.SelectedIndex;
                Toast.Show($"  已切换为 {ThemeManager.Instance.ThemeNames[StyleList.SelectedIndex]}", ToastType.Success);
            }
        }

        private void Mode_Changed(object s, SelectionChangedEventArgs e)
        {
            if (ModeList.SelectedIndex >= 0)
            {
                ThemeManager.Instance.CurrentMode = (AppColorMode)ModeList.SelectedIndex;
                Toast.Show($"  颜色模式: {ThemeManager.Instance.ModeNames[ModeList.SelectedIndex]}", ToastType.Success);
            }
        }

        private void ApplyHex(object s, RoutedEventArgs e)
        {
            var hex = HexInput.Text.Trim();
            if (string.IsNullOrEmpty(hex)) return;
            try
            {
                ColorConverter.ConvertFromString(hex);
                ThemeManager.Instance.SetCustomAccent(hex);
                Toast.Show("  强调色已更改", ToastType.Success);
            }
            catch
            {
                Toast.Show("  颜色格式错误，示例: #FF6B6B", ToastType.Error);
            }
        }

        private void ResetAccent(object s, RoutedEventArgs e)
        {
            ThemeManager.Instance.SetCustomAccent(null);
            HexInput.Text = "";
            Toast.Show("  已恢复默认强调色", ToastType.Success);
        }

        private void BrowseBg(object s, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "选择背景图片",
                Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                ThemeManager.Instance.SetCustomBgImage(dlg.FileName);
                BgPathText.Text = dlg.FileName;
                Toast.Show("  背景图片已设置", ToastType.Success);
            }
        }

        private void ClearBg(object s, RoutedEventArgs e)
        {
            ThemeManager.Instance.SetCustomBgImage(null);
            BgPathText.Text = "未设置";
            Toast.Show("  背景已清除", ToastType.Success);
        }

        private void BrowseSave(object s, RoutedEventArgs e)
        {
            var d = new FolderBrowserDialog { Description = "选择存档路径" };
            if (d.ShowDialog() == DialogResult.OK) SavePathBox.Text = d.SelectedPath;
        }

        private void BrowseCrash(object s, RoutedEventArgs e)
        {
            var d = new FolderBrowserDialog { Description = "选择崩溃日志路径" };
            if (d.ShowDialog() == DialogResult.OK) CrashPathBox.Text = d.SelectedPath;
        }

        private void BrowseGame(object s, RoutedEventArgs e)
        {
            var d = new FolderBrowserDialog { Description = "选择游戏安装路径" };
            if (d.ShowDialog() == DialogResult.OK) GamePathBox.Text = d.SelectedPath;
        }

        // ★ 三个重置方法
        private void ResetSavePath(object s, RoutedEventArgs e) { SavePathBox.Text = ""; }
        private void ResetCrashPath(object s, RoutedEventArgs e) { CrashPathBox.Text = ""; }
        private void ResetGamePath(object s, RoutedEventArgs e) { GamePathBox.Text = ""; }

        private void SavePaths(object s, RoutedEventArgs e)
        {
            GamePaths.SetCustomSavePath(string.IsNullOrWhiteSpace(SavePathBox.Text) ? null : SavePathBox.Text.Trim());
            GamePaths.SetCustomCrashPath(string.IsNullOrWhiteSpace(CrashPathBox.Text) ? null : CrashPathBox.Text.Trim());
            GamePaths.SetCustomGamePath(string.IsNullOrWhiteSpace(GamePathBox.Text) ? null : GamePathBox.Text.Trim());
            Logger.Log($"  路径已保存 - 存档:{GamePaths.SavePath} 崩溃:{GamePaths.CrashLogPath} 游戏:{GamePaths.GameInstallPath}");
            Toast.Show("  路径设置已保存", ToastType.Success);
        }
    }
}