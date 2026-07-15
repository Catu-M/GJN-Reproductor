using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MusicPlayer.Models;

namespace MusicPlayer.Converters;

public class LyricStateColorConverter : IValueConverter
{
    private static readonly SolidColorBrush PastBrush = new(Color.FromRgb(80, 80, 80));
    private static readonly SolidColorBrush ActiveBrush = new(Colors.White);
    private static readonly SolidColorBrush FutureBrush = new(Color.FromRgb(150, 150, 150));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LyricLineState state)
            return FutureBrush;

        return state switch
        {
            LyricLineState.Past => PastBrush,
            LyricLineState.Current => ActiveBrush,
            _ => FutureBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
