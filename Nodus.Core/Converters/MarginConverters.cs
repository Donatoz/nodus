using System;
using System.Globalization;
using Avalonia;

namespace Nodus.Core.Converters;

public class MarginTopConverter : Converter
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not float f ? default : new Thickness(0, f, 0 ,0);
    }
}
