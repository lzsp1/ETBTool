using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ETBTool.Utils;

namespace ETBTool.Windows
{
    public static class ThemedDialog
    {
        public static MessageBoxResult Show(Window owner, string message, string title = "", MessageBoxButton buttons = MessageBoxButton.OK)
        {
            var result = MessageBoxResult.None;

            var win = new Window
            {
                Width = 440,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false
            };

            var accent = (Brush)Application.Current.TryFindResource("AccentColor")
                         ?? new SolidColorBrush(Color.FromRgb(108, 92, 231));
            var textPrimary = (Brush)Application.Current.TryFindResource("TextPrimary") ?? Brushes.Black;
            var buttonBg = (Brush)Application.Current.TryFindResource("ButtonBackground") ?? Brushes.LightGray;
            var cardBg = (Brush)Application.Current.TryFindResource("CardBackground") ?? Brushes.White;
            var borderClr = (Brush)Application.Current.TryFindResource("BorderColor") ?? Brushes.Gray;

            var border = new Border
            {
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                BorderBrush = borderClr,
                Background = cardBg,
                Padding = new Thickness(28),
                Margin = new Thickness(8),
                Effect = new DropShadowEffect { BlurRadius = 20, ShadowDepth = 0, Opacity = 0.3, Color = Colors.Black }
            };

            var stack = new StackPanel();

            if (!string.IsNullOrEmpty(title))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = title,
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = textPrimary,
                    Margin = new Thickness(0, 0, 0, 14)
                });
            }

            stack.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = textPrimary,
                Margin = new Thickness(0, 0, 0, 24)
            });

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // ★ 使用 TryFindResource 获取样式
            Button MakeBtn(string text, MessageBoxResult r, bool isAccent)
            {
                var btn = new Button
                {
                    Content = text,
                    MinWidth = 80,
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                var style = Application.Current.TryFindResource(isAccent ? "AccentButton" : "BaseButton") as Style;
                if (style != null)
                    btn.Style = style;
                else
                {
                    btn.Padding = new Thickness(20, 10, 20, 10);
                    btn.FontSize = 13;
                    btn.Background = isAccent ? accent : buttonBg;
                    btn.Foreground = isAccent ? Brushes.White : textPrimary;
                    btn.BorderThickness = new Thickness(0);
                }

                btn.Click += (_, _) => { result = r; win.Close(); };
                return btn;
            }

            switch (buttons)
            {
                case MessageBoxButton.OK:
                    btnPanel.Children.Add(MakeBtn(LanguageManager.BtnConfirm, MessageBoxResult.OK, true));
                    break;
                case MessageBoxButton.OKCancel:
                    btnPanel.Children.Add(MakeBtn(LanguageManager.BtnConfirm, MessageBoxResult.OK, true));
                    btnPanel.Children.Add(MakeBtn(LanguageManager.BtnCancel, MessageBoxResult.Cancel, false));
                    break;
                case MessageBoxButton.YesNo:
                    btnPanel.Children.Add(MakeBtn(LanguageManager.BtnYes, MessageBoxResult.Yes, true));
                    btnPanel.Children.Add(MakeBtn(LanguageManager.BtnNo, MessageBoxResult.No, false));
                    break;
                case MessageBoxButton.YesNoCancel:
                    btnPanel.Children.Add(MakeBtn(LanguageManager.BtnYes, MessageBoxResult.Yes, true));
                    btnPanel.Children.Add(MakeBtn(LanguageManager.BtnNo, MessageBoxResult.No, false));
                    btnPanel.Children.Add(MakeBtn(LanguageManager.BtnCancel, MessageBoxResult.Cancel, false));
                    break;
            }

            stack.Children.Add(btnPanel);
            border.Child = stack;
            win.Content = border;

            border.MouseLeftButtonDown += (_, _) => { try { win.DragMove(); } catch { } };

            win.ShowDialog();
            return result;
        }
    }
}