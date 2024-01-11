using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Nodus.Core.Extensions;

namespace Nodus.NodeEditor.Views;

public partial class NodeCanvasToolbar : UserControl
{
    protected Panel ExtensionsPanel => RightPanel;
    
    public NodeCanvasToolbar()
    {
        InitializeComponent();
        
        Root.AddHandler(PointerEnteredEvent, OnPointerEnter);
        Root.AddHandler(PointerExitedEvent, OnPointerExit);
    }

    private void OnPointerExit(object? sender, PointerEventArgs e)
    {
        Container.SwitchBetweenClasses("active", "inactive", false);
    }

    private void OnPointerEnter(object? sender, PointerEventArgs e)
    {
        Container.SwitchBetweenClasses("active", "inactive", true);
    }
}