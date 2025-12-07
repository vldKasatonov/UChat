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

    public class TailLeftVisibilityConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Count < 2) return false;
            bool showTail = values[0] is bool b0 && b0;
            bool isMine = values[1] is bool b1 && b1;
            return showTail && !isMine;
        }
    }
    public class TailRightVisibilityConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Count < 2) return false;
            bool showTail = values[0] is bool b0 && b0;
            bool isMine = values[1] is bool b1 && b1;
            return showTail && isMine;
        }
    }
    public class TailMarginConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2)
                return new Avalonia.Thickness(0);

            bool showTail = values[0] is bool b0 && b0;
            bool isMine = values[1] is bool b1 && b1;
            
            if (showTail)
                return new Avalonia.Thickness(0);
            
            if (!isMine)
            {
                return new Avalonia.Thickness(8, 0, 0, 0);
            }
            else
            {
                return new Avalonia.Thickness(0, 0, 9, 0);
            }
        }
    }
}
