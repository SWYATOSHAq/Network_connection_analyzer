using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Анализатор_сетевых_подключений.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == "Up"
            ? new SolidColorBrush(Color.FromRgb(72, 187, 120))
            : new SolidColorBrush(Color.FromRgb(160, 174, 192));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
