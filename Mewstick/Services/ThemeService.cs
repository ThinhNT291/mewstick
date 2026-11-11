using System.Windows;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace Mewstick.Services;

/// <summary>
/// Đổi giao diện sáng/tối bằng cách thay các brush dùng chung trong Application.Resources
/// (giao diện tham chiếu chúng bằng DynamicResource nên tự cập nhật ngay).
/// </summary>
public static class ThemeService
{
    public static bool IsDark { get; private set; }

    // key -> (màu sáng, màu tối)
    private static readonly Dictionary<string, (string Light, string Dark)> Palette = new()
    {
        ["WindowBackgroundBrush"] = ("#FAFAF9", "#1E1E22"),
        ["TitleBarBrush"] = ("#F0F0EE", "#28282D"),
        ["TextPrimaryBrush"] = ("#2A2A2A", "#EDEDED"),
        ["TextMutedBrush"] = ("#9A9A9A", "#8F8F98"),
        ["InputBackgroundBrush"] = ("#F5F5F3", "#2A2A30"),
        ["InputBorderBrush"] = ("#DADAD5", "#3C3C44"),
        ["PlaceholderBrush"] = ("#9A9A95", "#80808A"),
        ["HelpPanelBrush"] = ("#F5F5F0", "#25252A"),
        ["MatrixPanelBrush"] = ("#F0F0EC", "#25252A"),
        ["PopupBackgroundBrush"] = ("#FFFFFF", "#2A2A30"),
        ["PopupBorderBrush"] = ("#CCCCCC", "#48484F"),
        ["PillBackgroundBrush"] = ("#FFFFFF", "#33333A"),
        ["EditBoxBrush"] = ("#20000000", "#40FFFFFF"),
        ["SoftButtonBackgroundBrush"] = ("#EEEEEE", "#3A3A42"),
        ["HoverBellBrush"] = ("#FFF3D6", "#4A4126"),
        ["HoverBoltBrush"] = ("#F3E5F5", "#43304A"),
        ["HoverTrashBrush"] = ("#FFEBEE", "#4D2D31"),
        ["QuickPanelBrush"] = ("#EDEDEB", "#2A2A30"),
        ["QuickBorderBrush"] = ("#B8B8B3", "#55555D"),
        ["QuickInputBorderBrush"] = ("#8A8A85", "#55555D"),
        ["QuickHintBrush"] = ("#6E6E68", "#9A9AA2"),
    };

    public static void Apply(bool dark)
    {
        IsDark = dark;

        var resources = System.Windows.Application.Current.Resources;
        foreach (var (key, colors) in Palette)
        {
            var color = (Color)System.Windows.Media.ColorConverter.ConvertFromString(dark ? colors.Dark : colors.Light);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            resources[key] = brush;
        }
    }
}
