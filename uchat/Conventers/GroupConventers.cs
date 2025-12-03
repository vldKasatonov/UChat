using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace uchat
{
    public class GroupAvatarVisibilityConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values == null || values.Count < 2)
            {
                return false;
            }
            bool isMine = values[0] as bool? ?? false;
            bool isGroup = values[1] as bool? ?? false;
            return isGroup && !isMine;
        }

        public object ConvertBack(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class GroupSenderVisibilityConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Count < 2)
            {
                return false;
            }
            bool isMine = values[0] is bool b1 && b1;
            bool isGroup = values[1] is bool b2 && b2;
            return isGroup && !isMine;
        }

        public object ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
