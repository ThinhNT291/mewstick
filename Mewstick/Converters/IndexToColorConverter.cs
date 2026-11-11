using System.Globalization;
using System.Windows.Data;
using Color = System.Windows.Media.Color;

namespace Mewstick.Converters;

/// <summary>
/// Chuyển chỉ số hàng (index) trong danh sách thành một màu pastel.
/// Mỗi hàng lệch màu (hue) đi một chút so với hàng trước, tạo hiệu ứng
/// dải màu chuyển dần liên tục khi cuộn xuống dưới.
/// </summary>
public class IndexToColorConverter : IValueConverter
{
    // Mỗi hàng lệch 18 độ hue; sau 20 hàng (20 * 18 = 360) thì màu lặp lại từ đầu.
    private const double HueStepDegrees = 18.0;
    // Giao diện sáng: pastel nhạt; giao diện tối: cùng dải màu nhưng đậm và trầm hơn để chữ sáng dễ đọc.
    private const double LightSaturation = 0.55;
    private const double LightLightness = 0.85;
    private const double DarkSaturation = 0.32;
    private const double DarkLightness = 0.26;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int index = value is int i ? i : 0;
        double hue = (index * HueStepDegrees) % 360.0;
        bool dark = Mewstick.Services.ThemeService.IsDark;
        return new System.Windows.Media.SolidColorBrush(
            HslToColor(hue, dark ? DarkSaturation : LightSaturation, dark ? DarkLightness : LightLightness));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static Color HslToColor(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60.0) % 2 - 1));
        double m = l - c / 2;

        double r1, g1, b1;
        if (h < 60) { r1 = c; g1 = x; b1 = 0; }
        else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
        else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
        else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
        else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
        else { r1 = c; g1 = 0; b1 = x; }

        byte r = (byte)Math.Round((r1 + m) * 255);
        byte g = (byte)Math.Round((g1 + m) * 255);
        byte b = (byte)Math.Round((b1 + m) * 255);
        return Color.FromRgb(r, g, b);
    }
}