using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ETBTool.Windows
{
    public partial class ThemedDialog : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        private ThemedDialog() { InitializeComponent(); Loaded += OnLoaded; }

        private void OnLoaded(object s, RoutedEventArgs e)
        {
            // 覆盖整个父窗口
            if (Owner != null) { Width = Owner.ActualWidth; Height = Owner.ActualHeight; Left = Owner.Left; Top = Owner.Top; }

            // 弹入动画
            DialogCard.Opacity = 0;
            var sc = new ScaleTransform(0.92, 0.92);
            DialogCard.RenderTransform = sc;
            DialogCard.RenderTransformOrigin = new Point(0.5, 0.5);
            DialogCard.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
            sc.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(250)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            sc.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(250)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        }

        public static MessageBoxResult Show(string msg, string title = "提示", MessageBoxButton btns = MessageBoxButton.OK)
            => Show(Application.Current?.MainWindow, msg, title, btns);

        public static MessageBoxResult Show(Window? owner, string msg, string title = "提示", MessageBoxButton btns = MessageBoxButton.OK)
        {
            if (owner == null) return MessageBox.Show(msg, title, btns);
            var d = new ThemedDialog { Owner = owner };
            d.TitleBlock.Text = title;
            d.MessageBlock.Text = msg;
            switch (btns)
            {
                case MessageBoxButton.OK: d.AddBtn("确定", true); break;
                case MessageBoxButton.YesNo: d.AddBtn("否", false); d.AddBtn("是", true); break;
                case MessageBoxButton.OKCancel: d.AddBtn("取消", false); d.AddBtn("确定", true); break;
                case MessageBoxButton.YesNoCancel: d.AddBtn("取消", false); d.AddBtn("否", false); d.AddBtn("是", true); break;
            }
            d.ShowDialog();
            return d.Result;
        }

        private void AddBtn(string text, bool primary)
        {
            var btn = new Button { Content = text, Padding = new Thickness(22, 10, 22, 10), Margin = new Thickness(8, 0, 0, 0), FontSize = 14, MinWidth = 80, Cursor = System.Windows.Input.Cursors.Hand };
            if (primary) { btn.SetResourceReference(BackgroundProperty, "AccentColor"); btn.Foreground = Brushes.White; btn.FontWeight = FontWeights.SemiBold; }
            else { btn.SetResourceReference(BackgroundProperty, "ButtonBackground"); btn.SetResourceReference(ForegroundProperty, "TextPrimary"); }

            var tpl = new ControlTemplate(typeof(Button));
            var bd = new FrameworkElementFactory(typeof(Border)) { Name = "bd" };
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            bd.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
            bd.SetResourceReference(Border.BackgroundProperty, primary ? "AccentColor" : "ButtonBackground");
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            tpl.VisualTree = bd;
            btn.Template = tpl;

            btn.Click += (_, _) => { Result = text == "确定" ? MessageBoxResult.OK : text == "是" ? MessageBoxResult.Yes : text == "否" ? MessageBoxResult.No : MessageBoxResult.Cancel; Close(); };
            ButtonPanel.Children.Add(btn);
        }
    }
}