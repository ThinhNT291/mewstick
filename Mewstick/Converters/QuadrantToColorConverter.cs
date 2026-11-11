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
            EisenhowerQuadrant.UrgentImportant => Color.FromRgb(0xEF, 0x53, 0x50),      // đỏ
            EisenhowerQuadrant.NotUrgentImportant => Color.FromRgb(0xAB, 0x47, 0xBC),   // tím
            EisenhowerQuadrant.UrgentNotImportant => Color.FromRgb(0xFF, 0xA7, 0x26),   // vàng
            EisenhowerQuadrant.NotUrgentNotImportant => Color.FromRgb(0x42, 0xA5, 0xF5),// xanh
            _ => Color.FromRgb(0x9A, 0x9A, 0x9A)                                        // xám cho "---"
        };
        return new System.Windows.Media.SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}