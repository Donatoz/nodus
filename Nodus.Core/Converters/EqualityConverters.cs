using System;
using System.Diagnostics;
using System.Globalization;

namespace Nodus.Core.Converters;

public class ReferenceEqualityConverter : Converter
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == parameter;
    }
}

public class ValueEqualityConverter : Converter
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.Equals(parameter) ?? false;
    }
}

public class ToStringEqualityConverter : Converter
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }
}