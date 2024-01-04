using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering.Composition;

namespace Nodus.Core.Controls;

public partial class LoadingOverlay : UserControl
{
    public LoadingOverlay()
    {
        InitializeComponent();
    }
}