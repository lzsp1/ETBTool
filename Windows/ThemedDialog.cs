using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ETBTool.Utils;

namespace ETBTool.Windows
{
    public static class ThemedDialog
    {
        public static MessageBoxResult Show(Window? owner, string message, string title, MessageBoxButton buttons = MessageBoxButton.OK,
            string? primaryLabel = null, string? secondaryLabel = null)
        {
            try
            {
                var result = MessageBoxResult.None;
                var dlg = CreateDialog(owner, title);
                var accent = TryGetBrush("AccentColor", "#6C5CE7");
                var bg = TryGetBrush("CardBackground", "#1a1a1a");
                var fg = TryGetBrush("TextPrimary", "#f0ece4");
                var fgSec = TryGetBrush("TextSecondary", "#7a7570");
                var borderColor = TryGetBrush("BorderColor", "#333");
                var btnBg = TryGetBrush("ButtonBackground", "#2a2a2a");

                var outerBorder = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    BorderThickness = new Thickness(1),
                    BorderBrush = borderColor,
                    Background = bg,
                    Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 0, Opacity = 0.3, Color = Colors.Black }
                };

                var stack = new StackPanel { Margin = new Thickness(28, 24, 28, 20) };
                stack.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = fg, Margin = new Thickness(0, 0, 0, 10) });
                stack.Children.Add(new ScrollViewer
                {
                    MaxHeight = Math.Max(120, SystemParameters.WorkArea.Height - 240),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(0, 0, 0, 20),
                    Content = new TextBlock { Text = message, FontSize = 13, Foreground = fgSec, TextWrapping = TextWrapping.Wrap, LineHeight = 22 }
                });
                dlg.PreviewKeyDown += (_, e) =>
                {
                    if (e.Key == Key.Escape) { result = MessageBoxResult.Cancel; e.Handled = true; dlg.Close(); }
                };

                var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

                Button MakeBtn(string text, bool primary, MessageBoxResult r)
                {
                    var btn = new Button
                    {
                        Content = text,
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Padding = new Thickness(20, 8, 20, 8),
                        Margin = new Thickness(8, 0, 0, 0),
                        Cursor = Cursors.Hand,
                        BorderThickness = new Thickness(0),
                        Background = primary ? accent : btnBg,
                        Foreground = primary ? Brushes.White : fg
                    };
                    btn.Click += (_, _) => { result = r; dlg.Close(); };
                    return btn;
                }

                switch (buttons)
                {
                    case MessageBoxButton.OK:
                        btnPanel.Children.Add(MakeBtn(primaryLabel ?? LanguageManager.BtnConfirm, true, MessageBoxResult.OK));
                        break;
                    case MessageBoxButton.OKCancel:
                        btnPanel.Children.Add(MakeBtn(primaryLabel ?? LanguageManager.BtnConfirm, true, MessageBoxResult.OK));
                        btnPanel.Children.Add(MakeBtn(secondaryLabel ?? LanguageManager.BtnCancel, false, MessageBoxResult.Cancel));
                        break;
                    case MessageBoxButton.YesNo:
                        btnPanel.Children.Add(MakeBtn(primaryLabel ?? LanguageManager.BtnYes, true, MessageBoxResult.Yes));
                        btnPanel.Children.Add(MakeBtn(secondaryLabel ?? LanguageManager.BtnNo, false, MessageBoxResult.No));
                        break;
                    case MessageBoxButton.YesNoCancel:
                        btnPanel.Children.Add(MakeBtn(primaryLabel ?? LanguageManager.BtnYes, true, MessageBoxResult.Yes));
                        btnPanel.Children.Add(MakeBtn(secondaryLabel ?? LanguageManager.BtnNo, false, MessageBoxResult.No));
                        btnPanel.Children.Add(MakeBtn(LanguageManager.BtnCancel, false, MessageBoxResult.Cancel));
                        break;
                }

                stack.Children.Add(btnPanel);
                outerBorder.Child = stack;
                dlg.Content = outerBorder;
                dlg.ShowDialog();
                return result;
            }
            catch { return MessageBox.Show(message, title, buttons); }
        }

        public static (MessageBoxResult result, bool isChecked) ShowCheckbox(
            Window? owner, string message, string title, string checkboxLabel,
            MessageBoxButton buttons = MessageBoxButton.OK)
        {
            try
            {
                var result = MessageBoxResult.None;
                var dlg = CreateDialog(owner, title);
                var accent = TryGetBrush("AccentColor", "#6C5CE7");
                var bg = TryGetBrush("CardBackground", "#1a1a1a");
                var fg = TryGetBrush("TextPrimary", "#f0ece4");
                var fgSec = TryGetBrush("TextSecondary", "#7a7570");
                var borderColor = TryGetBrush("BorderColor", "#333");
                var btnBg = TryGetBrush("ButtonBackground", "#2a2a2a");

                var outerBorder = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    BorderThickness = new Thickness(1),
                    BorderBrush = borderColor,
                    Background = bg,
                    Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 0, Opacity = 0.3, Color = Colors.Black }
                };

                var stack = new StackPanel { Margin = new Thickness(28, 24, 28, 20) };
                stack.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = fg, Margin = new Thickness(0, 0, 0, 10) });
                stack.Children.Add(new TextBlock { Text = message, FontSize = 13, Foreground = fgSec, TextWrapping = TextWrapping.Wrap, LineHeight = 22, Margin = new Thickness(0, 0, 0, 16) });
                var checkBox = new CheckBox { Content = checkboxLabel, FontSize = 12, Foreground = fgSec, Margin = new Thickness(0, 0, 0, 20) };
                stack.Children.Add(checkBox);

                var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

                Button MakeBtn(string text, bool primary, MessageBoxResult r)
                {
                    var btn = new Button
                    {
                        Content = text,
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Padding = new Thickness(20, 8, 20, 8),
                        Margin = new Thickness(8, 0, 0, 0),
                        Cursor = Cursors.Hand,
                        BorderThickness = new Thickness(0),
                        Background = primary ? accent : btnBg,
                        Foreground = primary ? Brushes.White : fg
                    };
                    btn.Click += (_, _) => { result = r; dlg.Close(); };
                    return btn;
                }

                switch (buttons)
                {
                    case MessageBoxButton.OK:
                        btnPanel.Children.Add(MakeBtn(LanguageManager.BtnConfirm, true, MessageBoxResult.OK));
                        break;
                    case MessageBoxButton.OKCancel:
                        btnPanel.Children.Add(MakeBtn(LanguageManager.BtnConfirm, true, MessageBoxResult.OK));
                        btnPanel.Children.Add(MakeBtn(LanguageManager.BtnCancel, false, MessageBoxResult.Cancel));
                        break;
                    case MessageBoxButton.YesNo:
                        btnPanel.Children.Add(MakeBtn(LanguageManager.BtnYes, true, MessageBoxResult.Yes));
                        btnPanel.Children.Add(MakeBtn(LanguageManager.BtnNo, false, MessageBoxResult.No));
                        break;
                    case MessageBoxButton.YesNoCancel:
                        btnPanel.Children.Add(MakeBtn(LanguageManager.BtnYes, true, MessageBoxResult.Yes));
                        btnPanel.Children.Add(MakeBtn(LanguageManager.BtnNo, false, MessageBoxResult.No));
                        btnPanel.Children.Add(MakeBtn(LanguageManager.BtnCancel, false, MessageBoxResult.Cancel));
                        break;
                }

                stack.Children.Add(btnPanel);
                outerBorder.Child = stack;
                dlg.Content = outerBorder;
                dlg.ShowDialog();
                return (result, checkBox.IsChecked == true);
            }
            catch { return (MessageBox.Show(message, title, buttons), false); }
        }

        private static Window CreateDialog(Window? owner, string title)
        {
            return new Window
            {
                Title = title,
                Width = 500,
                MinHeight = 160,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Owner = owner
            };
        }

        private static SolidColorBrush TryGetBrush(string key, string fallback)
        {
            try { var val = Application.Current?.TryFindResource(key); if (val is SolidColorBrush b) return b; } catch { }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(fallback));
        }
    }
}