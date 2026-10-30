using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ETBTool.Utils
{
    public enum ToastType { Success, Error, Info, Warning }

    public static class Toast
    {
        public static void Show(string message, ToastType type = ToastType.Info, int durationMs = 2500)
        {
            var win = Application.Current.MainWindow;
            if (win == null) return;

            win.Dispatcher.Invoke(() =>
            {
                var container = win.FindName("ToastOverlay") as Panel;
                if (container == null) return;

                var toast = CreateElement(message, type);
                container.Children.Add(toast);

                var tt = new TranslateTransform(300, 0);
                toast.RenderTransform = tt;
                toast.Opacity = 0;

                tt.BeginAnimation(TranslateTransform.XProperty,
                    new DoubleAnimation(300, 0, TimeSpan.FromMilliseconds(300))
                    { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                toast.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));

                var timer = new System.Windows.Threading.DispatcherTimer
                { Interval = TimeSpan.FromMilliseconds(durationMs) };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    tt.BeginAnimation(TranslateTransform.XProperty,
                        new DoubleAnimation(0, 300, TimeSpan.FromMilliseconds(250))
                        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } });
                    var fo = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
                    fo.Completed += (_, _) => container.Children.Remove(toast);
                    toast.BeginAnimation(UIElement.OpacityProperty, fo);
                };
                timer.Start();
            });
        }

        private static Border CreateElement(string msg, ToastType type)
        {
            var (bg, icon) = type switch
            {
                ToastType.Success => ("#2ECC71", "\u2713"),
                ToastType.Error => ("#E74C3C", "\u2717"),
                ToastType.Warning => ("#F39C12", "\u26A0"),
                _ => ("#6C5CE7", "\u2139"),
            };

            return new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 10, 20, 10),
                Margin = new Thickness(0, 4, 0, 0),
                MaxWidth = 400,
                HorizontalAlignment = HorizontalAlignment.Right,
                IsHitTestVisible = false,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                { BlurRadius = 16, ShadowDepth = 0, Opacity = 0.3, Color = Colors.Black },
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock { Text = icon, FontSize = 16, Foreground = Brushes.White,
                            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,10,0),
                            FontWeight = FontWeights.Bold },
                        new TextBlock { Text = msg, FontSize = 13, Foreground = Brushes.White,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 320 }
                    }
                }
            };
        }
    }
}