using Avalonia.Data.Converters;
using System.Globalization;

namespace uchat
{
    public class PasswordToggleMarginConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double actualWidth)
            {
                return new Avalonia.Thickness(actualWidth + 10, 0, 0, 5);
            }
            return new Avalonia.Thickness(0);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}