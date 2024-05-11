using System.Linq;
using Avalonia;
using Avalonia.ReactiveUI;
using Nodus.RenderEditor.ViewModels;
using Nodus.RenderEngine.Avalonia;
using Nodus.RenderEngine.OpenGL;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive.Disposables;

namespace Nodus.RenderEditor.Views;

public partial class RenderPreview : ReactiveUserControl<RenderPreviewViewModel>
{
    private GlRenderSurface? renderSurface;
    
    public RenderPreview()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            ViewModel?.Render.AlterationStream
                .Subscribe(x => renderSurface?.SwitchRendering(x))
                .DisposeWith(d);

            this.Bind(ViewModel, vm => vm.Render.MutableValue, v => v.RenderSwitch.IsChecked)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.UpdatePreview, v => v.UpdateButton)
                .DisposeWith(d);
        });
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        renderSurface = new GlRenderSurface
        {
            Width = 250, Height = 250, Margin = new Thickness(5),
            RendererFactory = PrimitiveRenderers.QuadRenderer,
            VertexShaderSource = "avares://Nodus.RenderEngine.Avalonia/Assets/Shaders/example.vert",
            FragmentShaderSource = "avares://Nodus.RenderEngine.Avalonia/Assets/Shaders/example.frag",
            Uniforms = UniformSets.TimerUniform.Concat(new []
            {
                new GlFloatUniform("mainTexture", () => 0, true),
                new GlFloatUniform("distortion", () => 1, true)
            }).ToArray(),
            TextureSources = new []
            {
                "avares://Nodus.RenderEngine.Avalonia/Assets/Textures/Noise_008.png",
                "avares://Nodus.RenderEngine.Avalonia/Assets/Textures/Noise_077.png"
            }
        };
        
        RenderContainer.Children.Add(renderSurface);
    }
}