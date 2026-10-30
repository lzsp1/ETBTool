using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ETBTool.Utils
{
    public static class ScrollBubble
    {
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached(
                "Enabled", typeof(bool), typeof(ScrollBubble),
                new PropertyMetadata(false, OnChanged));

        public static bool GetEnabled(DependencyObject obj) =>
            (bool)obj.GetValue(EnabledProperty);
        public static void SetEnabled(DependencyObject obj, bool val) =>
            obj.SetValue(EnabledProperty, val);

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement el)
            {
                if ((bool)e.NewValue)
                    el.PreviewMouseWheel += Handler;
                else
                    el.PreviewMouseWheel -= Handler;
            }
        }

        private static void Handler(object sender, MouseWheelEventArgs e)
        {
            if (sender is not UIElement root) return;

            // ★ HitTest 找到鼠标下方最内层的 ScrollViewer
            var pt = e.GetPosition(root);
            var hit = VisualTreeHelper.HitTest(root, pt);
            if (hit?.VisualHit == null) return;

            var innerSv = FindAncestor<ScrollViewer>(hit.VisualHit);
            if (innerSv == null) return;

            bool atTop = e.Delta > 0 && innerSv.VerticalOffset <= 0.5;
            bool atBottom = e.Delta < 0 &&
                innerSv.VerticalOffset >= innerSv.ExtentHeight - innerSv.ViewportHeight - 0.5;

            if (!atTop && !atBottom) return;

            // ★ 找到外层 ScrollViewer 并滚动
            var outerSv = FindAncestor<ScrollViewer>(
                VisualTreeHelper.GetParent(innerSv));
            if (outerSv == null) return;

            e.Handled = true;
            outerSv.ScrollToVerticalOffset(outerSv.VerticalOffset - e.Delta);
        }

        private static T? FindAncestor<T>(DependencyObject? obj) where T : DependencyObject
        {
            while (obj != null)
            {
                if (obj is T found) return found;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }
    }
}