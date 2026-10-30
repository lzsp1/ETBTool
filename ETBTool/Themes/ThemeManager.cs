using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ETBTool.Models;

namespace ETBTool.Themes
{
    public enum AppThemeStyle { Glass, Minimal, GlassMorphism, Neumorphism, ThreeD, Neon }
    public enum AppColorMode { Light, Dark, System }

    public class ThemeManager : INotifyPropertyChanged
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        private AppThemeStyle _style = AppThemeStyle.Minimal;
        private AppColorMode _mode = AppColorMode.Light;
        private bool _isSystemDark;
        private string? _customAccent;
        private string? _bgImagePath;

        private static readonly string[] StyleFiles =
        {
            "GlassTheme.xaml","MinimalTheme.xaml","GlassMorphismTheme.xaml",
            "NeumorphismTheme.xaml","ThreeDTheme.xaml","NeonTheme.xaml"
        };

        public AppThemeStyle CurrentStyle
        {
            get => _style;
            set { _style = value; OnPropertyChanged(nameof(CurrentStyle)); ApplyTheme(); }
        }

        public AppColorMode CurrentMode
        {
            get => _mode;
            set { _mode = value; OnPropertyChanged(nameof(CurrentMode)); ApplyTheme(); }
        }

        public bool IsCurrentlyDark => _mode switch
        {
            AppColorMode.Light => false,
            AppColorMode.Dark => true,
            AppColorMode.System => _isSystemDark,
            _ => false
        };

        public string[] ThemeNames => new[] { "全玻璃风格", "极简留白", "玻璃拟态", "新拟态", "3D立体风", "霓虹彩色" };
        public string[] ModeNames => new[] { "浅色", "深色", "跟随系统" };
        public string? BackgroundImagePath => _bgImagePath;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? ThemeChanged;

        private ThemeManager()
        {
            _isSystemDark = ReadSystemDark();
            SystemEvents.UserPreferenceChanged += (_, _) =>
            {
                if (_mode != AppColorMode.System) return;
                var now = ReadSystemDark();
                _isSystemDark = now;
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    ApplyTheme();
                    ThemeChanged?.Invoke();
                });
            };
        }

        public void LoadSavedTheme()
        {
            try
            {
                if (File.Exists(GamePaths.SettingsFile))
                {
                    var r = JsonDocument.Parse(File.ReadAllText(GamePaths.SettingsFile)).RootElement;
                    if (r.TryGetProperty("ThemeStyle", out var s))
                        _style = Enum.Parse<AppThemeStyle>(s.GetString() ?? "Minimal");
                    if (r.TryGetProperty("ColorMode", out var m))
                        _mode = Enum.Parse<AppColorMode>(m.GetString() ?? "Light");
                    if (r.TryGetProperty("CustomAccent", out var ca))
                        _customAccent = ca.GetString();
                    if (r.TryGetProperty("BgImagePath", out var bi))
                        _bgImagePath = bi.GetString();
                }
            }
            catch { }
            ApplyTheme();
        }

        public void SaveSettings()
        {
            try
            {
                var dir = Path.GetDirectoryName(GamePaths.SettingsFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var dict = new System.Collections.Generic.Dictionary<string, JsonElement>();
                if (File.Exists(GamePaths.SettingsFile))
                {
                    using var document = JsonDocument.Parse(File.ReadAllText(GamePaths.SettingsFile));
                    foreach (var p in document.RootElement.EnumerateObject())
                        dict[p.Name] = p.Value.Clone();
                }

                dict["ThemeStyle"] = JsonSerializer.SerializeToElement(_style.ToString());
                dict["ColorMode"] = JsonSerializer.SerializeToElement(_mode.ToString());
                dict["CustomAccent"] = JsonSerializer.SerializeToElement(_customAccent ?? "");
                dict["BgImagePath"] = JsonSerializer.SerializeToElement(_bgImagePath ?? "");

                File.WriteAllText(GamePaths.SettingsFile,
                    JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public void ApplyTheme()
        {
            var app = Application.Current;
            if (app == null) return;
            var rd = app.Resources;

            if (_mode == AppColorMode.System) _isSystemDark = ReadSystemDark();

            // 移除旧样式主题
            for (int i = rd.MergedDictionaries.Count - 1; i >= 0; i--)
            {
                var src = rd.MergedDictionaries[i].Source;
                if (src != null && Array.Exists(StyleFiles, f => f == Path.GetFileName(src.OriginalString)))
                    rd.MergedDictionaries.RemoveAt(i);
            }

            string uri = _style switch
            {
                AppThemeStyle.Glass => "Themes/GlassTheme.xaml",
                AppThemeStyle.Minimal => "Themes/MinimalTheme.xaml",
                AppThemeStyle.GlassMorphism => "Themes/GlassMorphismTheme.xaml",
                AppThemeStyle.Neumorphism => "Themes/NeumorphismTheme.xaml",
                AppThemeStyle.ThreeD => "Themes/ThreeDTheme.xaml",
                AppThemeStyle.Neon => "Themes/NeonTheme.xaml",
                _ => "Themes/MinimalTheme.xaml"
            };
            rd.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) });

            bool dark = IsCurrentlyDark;
            var pre = dark ? "Dark" : "Light";
            rd["WindowBackground"] = rd[$"{pre}Background"];
            rd["SurfaceColor"] = rd[$"{pre}Surface"];
            rd["TextPrimary"] = rd[$"{pre}TextPrimary"];
            rd["TextSecondary"] = rd[$"{pre}TextSecondary"];
            rd["BorderColor"] = rd[$"{pre}Border"];
            rd["ButtonBackground"] = rd[$"{pre}ButtonBg"];
            rd["ButtonHoverBackground"] = rd[$"{pre}ButtonHover"];
            rd["CardBackground"] = rd[$"{pre}CardBg"];
            rd["CardHoverBackground"] = rd[$"{pre}CardHover"];
            rd["SidebarBackground"] = rd[$"{pre}Sidebar"];
            rd["HoverTintColor"] = rd[$"{pre}HoverTintColor"];

            // 强调色处理
            if (!string.IsNullOrWhiteSpace(_customAccent))
            {
                try
                {
                    rd["AccentColor"] = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(_customAccent));
                }
                catch { }
            }
            else
            {
                rd.Remove("AccentColor");
            }

            // 图片背景
            if (!string.IsNullOrEmpty(_bgImagePath) && File.Exists(_bgImagePath))
            {
                try
                {
                    var bmp = new BitmapImage(new Uri(_bgImagePath, UriKind.Absolute));
                    rd["WindowBackground"] = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
                }
                catch { }
            }

            SaveSettings();
        }

        public void SetCustomAccent(string? hex)
        {
            _customAccent = string.IsNullOrWhiteSpace(hex) ? null : hex;
            ApplyTheme();
            ThemeChanged?.Invoke();
        }

        public void SetCustomBgImage(string? path)
        {
            _bgImagePath = path;
            ApplyTheme();
            ThemeChanged?.Invoke();
        }

        private bool ReadSystemDark()
        {
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return k?.GetValue("AppsUseLightTheme") is int v && v == 0;
            }
            catch { return true; }
        }

        protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
