using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FlowEditor.Views;
using Nodus.Core.Interaction;
using Nodus.DI.Runtime;
using Nodus.RenderEngine.Avalonia;
using Nodus.RenderEngine.OpenGL;
using Nodus.ViewModels;

namespace Nodus.App.Views;

public partial class MainWindow : Window
{
    protected IRuntimeElementProvider ElementProvider { get; }
    
    public MainWindow(IRuntimeElementProvider elementProvider)
    {
        ElementProvider = elementProvider;
        
        InitializeComponent();
        
        this.AttachDevTools();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is MainWindowViewModel vm)
        {
            var canvas = ElementProvider.GetRuntimeElement<FlowCanvas>();
            canvas.DataContext = vm.CanvasViewModel;
            //Container.Children.Insert(0, canvas);
        }
    }

    private void OnContainerAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        var renderSurface = new GlRenderSurface
        {
            Width = 400, Height = 400,
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
        
        var binder = new WindowHotkeyBinder(this);
        binder.BindHotkey(KeyGesture.Parse("Space"), () => renderSurface.UpdateShaders());
        
        Container.Children.Add(renderSurface);
    }
}