using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Nodus.Core.Extensions;

namespace Nodus.Core.Controls;

public class DataContextControlBinding : IDisposable
{
    private readonly StyledElement observedElement;
    private readonly IEnumerable<(StyledElement element, Func<object?>)> boundElements;
    
    public DataContextControlBinding(StyledElement observedElement, params (StyledElement element, Func<object?> contextGetter)[] boundElements)
    {
        this.boundElements = boundElements;
        observedElement.DataContextChanged += OnDataContextChanged;
        this.observedElement = observedElement;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        boundElements.ForEach(x => x.element.DataContext = x.Item2.Invoke());
    }

    public void Dispose()
    {
        observedElement.DataContextChanged -= OnDataContextChanged;
    }
}