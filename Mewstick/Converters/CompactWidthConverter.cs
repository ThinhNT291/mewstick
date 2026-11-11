using System.Globalization;
using System.Windows.Data;

namespace Mewstick.Converters;

/// <summary>True khi chiều rộng cửa sổ nhỏ hơn ngưỡng (chế độ gọn: nhãn ưu tiên thu thành ngôi sao).</summary>
public class CompactWidthConverter : IValueConverter
{
    private const double Threshold = 340;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double width && width < Threshold;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
