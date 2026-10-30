using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ETBTool.Models;
using ETBTool.Services;
using System.Windows.Automation;
using ETBTool.Themes;
using ETBTool.Utils;
using ETBTool.Windows;
using Microsoft.Win32;
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

        private bool _refreshingText = true;
        private bool _refreshingMusic;
        private readonly BackgroundMusicService _music = BackgroundMusicService.Instance;

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            SavePathBox.Text = GamePaths.GetRawCustomSavePath() ?? "";
            CrashPathBox.Text = GamePaths.GetRawCustomCrashPath() ?? "";
            GamePathBox.Text = GamePaths.GetRawCustomGamePath() ?? "";
            BgPathText.Text = ThemeManager.Instance.BackgroundImagePath ?? LanguageManager.SetBgNone;

            LangList.Items.Add(LanguageManager.LangChinese);
            LangList.Items.Add(LanguageManager.LangEnglish);
            LangList.SelectedIndex = LanguageManager.IsChinese ? 0 : 1;

            StyleList.Items.Clear();
            foreach (var n in LanguageManager.ThemeNames) StyleList.Items.Add(n);
            StyleList.SelectedIndex = (int)ThemeManager.Instance.CurrentStyle;

            ModeList.Items.Clear();
            foreach (var n in LanguageManager.ModeNames) ModeList.Items.Add(n);
            ModeList.SelectedIndex = (int)ThemeManager.Instance.CurrentMode;

            LoadSwatches();
            RefreshText();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LanguageManager.LanguageChanged -= RefreshText;
            LanguageManager.LanguageChanged += RefreshText;
            _music.Changed -= RefreshMusic;
            _music.Changed += RefreshMusic;
            RefreshText();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            LanguageManager.LanguageChanged -= RefreshText;
            _music.Changed -= RefreshMusic;
        }

        private void RefreshText()
        {
            _refreshingText = true;
            PageTitle.Text = LanguageManager.SetTitle;
            PageSub.Text = LanguageManager.SetSub;
            SetLangTitle.Text = LanguageManager.SetLanguageLabel;
            SetLangDesc.Text = LanguageManager.SetLangDesc;
            SetStyleTitle.Text = LanguageManager.SetStyle;
            SetModeTitle.Text = LanguageManager.SetMode;
            SetAccentTitle.Text = LanguageManager.SetAccent;
            SetAccentDesc.Text = LanguageManager.SetAccentDesc;
            HexLabel.Text = LanguageManager.SetHexLabel;
            BtnApplyHex.Content = LanguageManager.BtnApply;
            BtnResetAccent.Content = LanguageManager.BtnReset;
            SetBgTitle.Text = LanguageManager.SetBg;
            SetBgDesc.Text = LanguageManager.SetBgDesc;
            BtnChooseBg.Content = LanguageManager.BtnChooseImg;
            BtnClearBg.Content = LanguageManager.BtnClearBg;
            SetPathsTitle.Text = LanguageManager.SetPaths;
            LblSave.Text = LanguageManager.SetSaveLabel;
            LblCrash.Text = LanguageManager.SetCrashLabel;
            LblGame.Text = LanguageManager.SetGameLabel;
            BtnBrowseSave.Content = LanguageManager.BtnBrowse;
            BtnBrowseCrash.Content = LanguageManager.BtnBrowse;
            BtnBrowseGame.Content = LanguageManager.BtnBrowse;
            BtnResetSave.Content = LanguageManager.BtnReset;
            BtnResetCrash.Content = LanguageManager.BtnReset;
            BtnResetGame.Content = LanguageManager.BtnReset;
            BtnSavePaths.Content = LanguageManager.BtnSavePaths;

            var si = StyleList.SelectedIndex;
            StyleList.Items.Clear();
            foreach (var n in LanguageManager.ThemeNames) StyleList.Items.Add(n);
            StyleList.SelectedIndex = si >= 0 && si < StyleList.Items.Count ? si : 0;

            var mi = ModeList.SelectedIndex;
            ModeList.Items.Clear();
            foreach (var n in LanguageManager.ModeNames) ModeList.Items.Add(n);
            ModeList.SelectedIndex = mi >= 0 && mi < ModeList.Items.Count ? mi : 0;

            var li = LangList.SelectedIndex;
            LangList.Items.Clear();
            LangList.Items.Add(LanguageManager.LangChinese);
            LangList.Items.Add(LanguageManager.LangEnglish);
            LangList.SelectedIndex = li >= 0 && li < LangList.Items.Count ? li : 0;
            BgPathText.Text = ThemeManager.Instance.BackgroundImagePath ?? LanguageManager.SetBgNone;
            _refreshingText = false;
            RefreshMusic();
        }

        private void RefreshMusic()
        {
            _refreshingMusic = true;
            bool chinese = LanguageManager.IsChinese;
            SetMusicTitle.Text = chinese ? "背景音乐" : "Background music";
            ChooseMusicText.Text = chinese ? "选择音乐" : "Choose music";
            MusicTrackText.Text = _music.HasTrack ? _music.TrackName : (chinese ? "未选择音乐" : "No music selected");
            MusicTrackText.ToolTip = _music.HasTrack ? _music.TrackName : null;
            MusicStatusText.Text = _music.IsImporting ? (chinese ? "正在导入…" : "Importing...") : _music.State switch
            {
                MusicPlaybackState.Loading => chinese ? "正在加载音频…" : "Loading audio...",
                MusicPlaybackState.Playing => chinese ? "正在循环播放" : "Playing on repeat",
                MusicPlaybackState.Paused => chinese ? "已暂停" : "Paused",
                MusicPlaybackState.Error => chinese ? "音频不可用" : "Audio unavailable",
                _ => chinese ? "未播放" : "Not playing"
            };
            bool playing = _music.State is MusicPlaybackState.Playing or MusicPlaybackState.Loading;
            PlayMusicIcon.Text = playing ? "\uE769" : "\uE768";
            BtnPlayMusic.ToolTip = playing ? (chinese ? "暂停" : "Pause") : (chinese ? "播放" : "Play");
            BtnClearMusic.ToolTip = chinese ? "清除音乐" : "Clear music";
            AutomationProperties.SetName(BtnChooseMusic, ChooseMusicText.Text);
            AutomationProperties.SetName(BtnPlayMusic, (string)BtnPlayMusic.ToolTip);
            AutomationProperties.SetName(BtnClearMusic, (string)BtnClearMusic.ToolTip);
            BtnChooseMusic.IsEnabled = !_music.IsImporting;
            BtnPlayMusic.IsEnabled = _music.HasTrack && !_music.IsImporting;
            BtnClearMusic.IsEnabled = (_music.HasTrack || _music.ErrorMessage != null) && !_music.IsImporting;
            MusicVolumeLabel.Text = chinese ? "音量" : "Volume";
            AutomationProperties.SetName(MusicVolume, MusicVolumeLabel.Text);
            MusicVolume.Value = _music.Volume * 100;
            MusicVolumeText.Text = $"{MusicVolume.Value:0}%";
            MusicErrorText.Visibility = _music.ErrorMessage == null ? Visibility.Collapsed : Visibility.Visible;
            MusicErrorText.Text = _music.ErrorMessage == null ? "" :
                (_music.State == MusicPlaybackState.Error
                    ? (chinese ? "无法播放。文件可能损坏或 Windows 缺少所需音频解码器。" : "Cannot play this file. It may be damaged or require a Windows audio codec.")
                    : (chinese ? "音乐设置未能保存或导入。" : "Music could not be imported or saved.")) + "\n" + _music.ErrorMessage;
            _refreshingMusic = false;
        }

        private async void BrowseMusic(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = LanguageManager.IsChinese ? "选择背景音乐" : "Choose background music",
                Filter = "Audio|*.mp3;*.wav;*.wma;*.aac;*.m4a;*.flac|MP3|*.mp3|WAV|*.wav|WMA|*.wma|AAC / M4A|*.aac;*.m4a|FLAC|*.flac",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog() == true) await _music.ImportAsync(dialog.FileName);
        }

        private void ToggleMusic(object sender, RoutedEventArgs e) => _music.TogglePlayback();
        private void ClearMusic(object sender, RoutedEventArgs e) => _music.Clear();

        private void MusicVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_refreshingMusic || MusicVolumeText == null) return;
            _music.SetVolume(e.NewValue / 100);
            MusicVolumeText.Text = $"{e.NewValue:0}%";
        }

        private void LoadSwatches()
        {
            ColorSwatches.Children.Clear();
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
                    Logger.Log($"强调色: {c}");
                    Toast.Show(LanguageManager.MsgAccentChanged, ToastType.Success);
                };
                ColorSwatches.Children.Add(swatch);
            }
        }

        private void Lang_Changed(object s, SelectionChangedEventArgs e)
        {
            if (_refreshingText || LangList.SelectedIndex < 0) return;
            LanguageManager.SetLanguage(LangList.SelectedIndex == 0);
        }

        private void Style_Changed(object s, SelectionChangedEventArgs e)
        {
            if (_refreshingText || StyleList.SelectedIndex < 0) return;
            ThemeManager.Instance.CurrentStyle = (AppThemeStyle)StyleList.SelectedIndex;
            Toast.Show(LanguageManager.MsgStyleChanged(
                LanguageManager.ThemeNames[StyleList.SelectedIndex]), ToastType.Success);
        }

        private void Mode_Changed(object s, SelectionChangedEventArgs e)
        {
            if (_refreshingText || ModeList.SelectedIndex < 0) return;
            ThemeManager.Instance.CurrentMode = (AppColorMode)ModeList.SelectedIndex;
            Toast.Show(LanguageManager.MsgModeChanged(
                LanguageManager.ModeNames[ModeList.SelectedIndex]), ToastType.Success);
        }

        private void ApplyHex(object s, RoutedEventArgs e)
        {
            var hex = HexInput.Text.Trim();
            if (string.IsNullOrEmpty(hex)) return;
            try
            {
                ColorConverter.ConvertFromString(hex);
                ThemeManager.Instance.SetCustomAccent(hex);
                Toast.Show(LanguageManager.MsgAccentChanged, ToastType.Success);
            }
            catch { Toast.Show(LanguageManager.MsgHexError, ToastType.Error); }
        }

        private void ResetAccent(object s, RoutedEventArgs e)
        {
            ThemeManager.Instance.SetCustomAccent(null);
            HexInput.Text = "";
            Toast.Show(LanguageManager.MsgAccentReset, ToastType.Success);
        }

        private void BrowseBg(object s, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = LanguageManager.SetBg,
                Filter = "Image|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                ThemeManager.Instance.SetCustomBgImage(dlg.FileName);
                BgPathText.Text = dlg.FileName;
                Toast.Show(LanguageManager.MsgBgSet, ToastType.Success);
            }
        }

        private void ClearBg(object s, RoutedEventArgs e)
        {
            ThemeManager.Instance.SetCustomBgImage(null);
            BgPathText.Text = LanguageManager.SetBgNone;
            Toast.Show(LanguageManager.MsgBgClear, ToastType.Success);
        }

        private void BrowseSave(object s, RoutedEventArgs e)
        { var d = new System.Windows.Forms.FolderBrowserDialog(); if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) SavePathBox.Text = d.SelectedPath; }
        private void BrowseCrash(object s, RoutedEventArgs e)
        { var d = new System.Windows.Forms.FolderBrowserDialog(); if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) CrashPathBox.Text = d.SelectedPath; }
        private void BrowseGame(object s, RoutedEventArgs e)
        { var d = new System.Windows.Forms.FolderBrowserDialog(); if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK) GamePathBox.Text = d.SelectedPath; }

        private void ResetSavePath(object s, RoutedEventArgs e) { SavePathBox.Text = ""; }
        private void ResetCrashPath(object s, RoutedEventArgs e) { CrashPathBox.Text = ""; }
        private void ResetGamePath(object s, RoutedEventArgs e) { GamePathBox.Text = ""; }

        private void SavePaths(object s, RoutedEventArgs e)
        {
            GamePaths.SetCustomSavePath(string.IsNullOrWhiteSpace(SavePathBox.Text) ? null : SavePathBox.Text.Trim());
            GamePaths.SetCustomCrashPath(string.IsNullOrWhiteSpace(CrashPathBox.Text) ? null : CrashPathBox.Text.Trim());
            GamePaths.SetCustomGamePath(string.IsNullOrWhiteSpace(GamePathBox.Text) ? null : GamePathBox.Text.Trim());
            Toast.Show(LanguageManager.MsgPathsSaved, ToastType.Success);
        }
    }
}