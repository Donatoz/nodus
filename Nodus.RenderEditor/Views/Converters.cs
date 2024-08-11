using System;
using System.Diagnostics;
using System.Globalization;
using Nodus.Core.Converters;
using Nodus.RenderEditor.Meta;

namespace Nodus.RenderEditor.Views;

public class IsRenderMetadataTypeConverter : Converter
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Type t && typeof(IRenderMetadata).IsAssignableFrom(t);
    }
}