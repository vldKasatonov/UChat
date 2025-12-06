using Avalonia.Data.Converters;
using System.Globalization;
namespace uchat;

public class ShutdownTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b
            ? "Something went wrong.\nPlease restart the application."
            : "Oops! Connection lost.\nTrying to reconnect...";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}