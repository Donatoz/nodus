using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using DynamicData;

namespace Nodus.NodeEditor.Views;

public partial class NodeCanvasToolbar : UserControl
{
    public NodeCanvasToolbar()
    {
        InitializeComponent();
        
        Root.AddHandler(PointerEnteredEvent, OnPointerEnter);
        Root.AddHandler(PointerExitedEvent, OnPointerExit);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        Hide();
    }

    private void OnPointerExit(object? sender, PointerEventArgs e)
    {
        Hide();
    }

    private void OnPointerEnter(object? sender, PointerEventArgs e)
    {
        Show();
    }

    public void Show()
    {
        Container.Classes.Remove("inactive");
        Container.Classes.Add("active");
    }

    public void Hide()
    {
        Container.Classes.Remove("active");
        Container.Classes.Add("inactive");
    }
}