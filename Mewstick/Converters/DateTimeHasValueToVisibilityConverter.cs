using System.Globalization;
using System.Windows.Data;

namespace Mewstick.Converters;

/// <summary>Có giá trị ngày/giờ (khác null) -> true, ngược lại -> false.</summary>
public class DateTimeHasValueToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DateTime;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}