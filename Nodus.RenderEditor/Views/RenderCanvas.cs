using System;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Nodus.Core.Controls;
using Nodus.Core.Extensions;
using Nodus.Core.ViewModels;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Views;
using Nodus.RenderEditor.ViewModels;

namespace Nodus.RenderEditor.Views;

public class RenderCanvas : NodeCanvas
{
    protected override string GraphType => "Render Graph";

    public RenderCanvas(IFactoryProvider<NodeCanvas> factoryProvider) : base(factoryProvider)
    {
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        Styles.Add(new StyleInclude((Uri?)null)
        {
            Source = new Uri("avares://Nodus.RenderEditor/Styles/RenderEditorStyles.axaml")
        });
        
        RightExtensionsContainer.Children.Add(this.CreateExtensionControl<RenderPreview, RenderPreviewViewModel>());
        
        /*
        var contentBrowser = this.CreateExtensionControl<ContentBrowser, ContentBrowserViewModel>();
        contentBrowser.Margin = new Thickness(15);
        BottomExtensionsContainer.Children.Add(contentBrowser);
        */
    }
}