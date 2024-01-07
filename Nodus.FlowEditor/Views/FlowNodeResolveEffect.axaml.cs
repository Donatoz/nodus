using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Nodus.Core.Extensions;

namespace FlowEditor.Views;

public partial class FlowNodeResolveEffect : UserControl
{
    public FlowNodeResolveEffect()
    {
        InitializeComponent();
    }

    public void SwitchState(bool isShown)
    {
        Effect.SwitchBetweenClasses("visible", "invisible", isShown);
        EffectBorder.SwitchBetweenClasses("visible", "invisible", isShown);
    }
}