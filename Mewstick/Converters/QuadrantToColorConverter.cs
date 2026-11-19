using System.Globalization;
using System.Windows.Data;
using Mewstick.Models;
using Color = System.Windows.Media.Color;

namespace Mewstick.Converters;

/// <summary>Màu chữ cố định theo mức ưu tiên: đỏ (Gấp&amp;QT) > tím (QT) > vàng (Gấp) > xanh (còn lại).</summary>
public class QuadrantToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Color color = value switch
        {
            EisenhowerQuadrant.UrgentImportant => Color.FromRgb(0xF4, 0x3F, 0x5E),      // đỏ hồng
            EisenhowerQuadrant.NotUrgentImportant => Color.FromRgb(0x8B, 0x5C, 0xF6),   // tím
            EisenhowerQuadrant.UrgentNotImportant => Color.FromRgb(0xF5, 0x9E, 0x0B),   // hổ phách
            EisenhowerQuadrant.NotUrgentNotImportant => Color.FromRgb(0x3B, 0x82, 0xF6),// xanh dương
            _ => Color.FromRgb(0x9A, 0x9A, 0x9A)                                        // xám cho "---"
        };
        return new System.Windows.Media.SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}