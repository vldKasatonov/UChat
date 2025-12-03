using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace uchat
{
    public class SenderToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string sender)
            {
                int hash = sender.GetHashCode();
                byte r = (byte)((hash & 0xFF0000) >> 16);
                byte g = (byte)((hash & 0x00FF00) >> 8);
                byte b = (byte)(hash & 0x0000FF);
                return new SolidColorBrush(Color.FromRgb(r, g, b));
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
