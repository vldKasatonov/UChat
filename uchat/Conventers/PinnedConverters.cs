using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using Avalonia.Platform;
using System.IO;

namespace uchat;

public class PinnedToIconConverter : IValueConverter
{
    private static readonly Bitmap PinImage = 
        new Bitmap(AssetLoader.Open(new Uri("avares://uchat/Resources/images/pin.png")));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPinned && isPinned)
        {
            return PinImage;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PinMenuTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isPinned)
        {
            return isPinned ? "Unpin chat" : "Pin chat";
        }
        return "Pin chat";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}