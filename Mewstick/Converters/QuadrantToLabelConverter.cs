using System.Globalization;
using System.Windows.Data;
using Mewstick.Models;

namespace Mewstick.Converters;

/// <summary>Chuyển enum ưu tiên thành nhãn chữ ngắn hiển thị trên hàng task.</summary>
public class QuadrantToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            EisenhowerQuadrant.UrgentImportant => "Gấp & Quan trọng",
            EisenhowerQuadrant.UrgentNotImportant => "Gấp",
            EisenhowerQuadrant.NotUrgentImportant => "Quan trọng",
            _ => "---"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}