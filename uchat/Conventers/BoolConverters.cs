using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using System.Globalization;

namespace uchat
{
    public class BoolToAlignment : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && b) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Avalonia.Data.BindingOperations.DoNothing;
    }

    public class BoolToBrush : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => (value is bool b && b) ? Brush.Parse("#B56B9C") :Brush.Parse("#45335D");
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Avalonia.Data.BindingOperations.DoNothing;
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : true;
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool b ? !b : true;
    }
}
