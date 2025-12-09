using Avalonia.Data.Converters;
using System.Globalization;

namespace uchat
{
    public class SentTimeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt && dt != default)
                return dt.ToString("HH:mm");
            return "";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

