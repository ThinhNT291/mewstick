using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Point = System.Windows.Point;

namespace Mewstick.Controls;

/// <summary>
/// StackPanel tự phát hiện khi 1 item đổi vị trí (do ObservableCollection.Move,
/// ví dụ khi tick/bỏ tick task khiến danh sách sắp xếp lại) và chạy hiệu ứng
/// trượt mượt tới vị trí mới, thay vì "biến mất - xuất hiện" đột ngột.
/// </summary>
public class AnimatedStackPanel : StackPanel
{
    private readonly Dictionary<UIElement, double> _previousTops = new();

    public AnimatedStackPanel()
    {
        LayoutUpdated += OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        foreach (UIElement child in InternalChildren)
        {
            if (!child.IsVisible)
            {
                _previousTops.Remove(child);
                continue;
            }

            if (child is not FrameworkElement fe) continue;
            double top = LayoutInformation.GetLayoutSlot(fe).Top;

            if (_previousTops.TryGetValue(child, out var previousTop))
            {
                double delta = previousTop - top;
                if (Math.Abs(delta) > 1)
                {
                    if (child.RenderTransform is not TranslateTransform transform)
                    {
                        transform = new TranslateTransform();
                        child.RenderTransform = transform;
                    }

                    transform.BeginAnimation(TranslateTransform.YProperty, null);
                    transform.Y = delta;

                    var animation = new DoubleAnimation(delta, 0, TimeSpan.FromMilliseconds(220))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    transform.BeginAnimation(TranslateTransform.YProperty, animation);
                }
            }

            _previousTops[child] = top;
        }

        // Dọn các phần tử đã bị gỡ khỏi panel (task bị xoá, hàng được dựng lại khi đổi giao diện).
        if (_previousTops.Count > InternalChildren.Count)
        {
            var alive = new HashSet<UIElement>(InternalChildren.Cast<UIElement>());
            foreach (var stale in _previousTops.Keys.Where(k => !alive.Contains(k)).ToList())
                _previousTops.Remove(stale);
        }
    }
}