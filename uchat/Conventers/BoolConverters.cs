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

     public class BoolToMarginConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isMine)
            {
                if (!isMine)
                {
                    return new Avalonia.Thickness(8, 5, 3, 5); 
                }
                else
                {
                    return new Avalonia.Thickness(3, 5, 3, 5);
                }
            }
            return new Avalonia.Thickness(3, 5, 3, 5);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    
    public class MessageMarginConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            bool showTail = values.Count > 0 && values[0] is bool b0 && b0;
            bool isMine = values.Count > 1 && values[1] is bool b1 && b1;
            bool isGroup = values.Count > 2 && values[2] is bool b2 && b2;
            double avatarWidth = 35; 
            if (values.Count > 3 && values[3] is double d) avatarWidth = d;
            double tailOffset = 6.5;      
            double avatarSpacing = 4;    
            double extraPadding = 4;    

            if (showTail)
                return new Avalonia.Thickness(0);

            if (!isMine) 
            {
                if (isGroup)
                {
                    return new Avalonia.Thickness( avatarSpacing + tailOffset, 0, 0, 0);
                }
                else
                {
                    return new Avalonia.Thickness(tailOffset + extraPadding, 0, 0, 0);
                }
            }
            else 
            {
                return new Avalonia.Thickness(0, 0, tailOffset + extraPadding, 0);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
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
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            bool showTail = values.Count > 0 && values[0] is bool b0 && b0;
            bool isMine = values.Count > 1 && values[1] is bool b1 && b1;
            bool isGroup = values.Count > 2 && values[2] is bool b2 && b2;
            double avatarWidth = 40;
            if (values.Count > 3 && values[3] is double d) avatarWidth = d;

            double tailBase = 4.5; 

            if (showTail)
                return new Avalonia.Thickness(0);

            if (!isMine)
            {
                if (isGroup)
                    return new Avalonia.Thickness(tailBase + avatarWidth, 0, 0, 0);
                else
                    return new Avalonia.Thickness(tailBase, 0, 0, 0);
            }
            else
            {
                return new Avalonia.Thickness(0, 0, tailBase, 0);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}


