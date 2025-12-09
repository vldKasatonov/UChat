using Avalonia.Data.Converters;
using System.Globalization;

namespace uchat;

public class DynamicMaxWidthConverter : IValueConverter
{
    private const double NarrowThreshold = 750;
    private const double MediumThreshold = 1100;
    private const double NarrowMaxWidth = 300;
    private const double MediumMaxWidth = 600;
    private const double WideMaxWidth = 700;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double parentWidth)
        {
            if (parentWidth >= MediumThreshold)
            {
                return WideMaxWidth;
            }
            else if (parentWidth >= NarrowThreshold)
            {
                return MediumMaxWidth;
            }
            else
            {
                return NarrowMaxWidth;
            }
        }
        return NarrowMaxWidth;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}