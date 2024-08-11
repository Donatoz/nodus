using System.Drawing;
using ImGuiNET;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.OpenGL;
using Nodus.RenderEngine.OpenGL.Convention;
using Nodus.RenderEngine.Serialization;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Nodus.VisualTests;

public class GlWindow
{
    private readonly IWindow window;
    private readonly GlSceneRenderer renderer;
    
    private GL? gl;
    private IInputContext? input;
    private ImGuiController? gui;

    private IGeometryFactory? geoFactory;

    private GlRenderObject? sceneObject;
    private GlRenderObject? anotherObject;
    private RenderScene? scene;
    private IScreenViewer? viewer;

    private IGlTexture[]? textures;
    private IGlMaterialDefinition? defaultMaterial;

    private float timeMultiplier = 1;
    private bool isOrthographic;
    private float fov = 45;

    public GlWindow()
    {
        var opt = WindowOptions.Default;
        opt.Size = new Vector2D<int>(800, 600);
        opt.Title = "Nodus Renderer (OpenGL)";

        renderer = new GlSceneRenderer(GetFallbackMaterial());

        window = Window.Create(opt);
        
        window.Load += OnLoad;
        window.Render += OnRender;
        window.FramebufferResize += OnFrameBufferResize;
        window.Update += OnUpdate;
    }

    public void Run()
    {
        window.Run();
        window.Dispose();
    }

    private void OnLoad()
    {
        gl = GL.GetApi(window);
        input = window!.CreateInput();
        
        textures = new IGlTextureDefinition[]
        {
            new GlTextureDefinition(new TextureFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Common\Nodus.RenderEngine.Avalonia\Assets\Textures\Noise_008.png"), 
                new GlTextureSpecification(TextureUnit.Texture0, TextureTarget.Texture2D)),
            new GlTextureDefinition(new TextureFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Common\Nodus.RenderEngine.Avalonia\Assets\Textures\Noise_077.png"), 
                new GlTextureSpecification(TextureUnit.Texture1, TextureTarget.Texture2D))
        }.Select(x => new GlTexture(gl, x.Source, x.Specification, renderer!)).OfType<IGlTexture>().ToArray();

        defaultMaterial = GetSampleMaterial();
        geoFactory = new AssimpGeometryFactory();
        
        var cube = geoFactory.CreateFromFile(@"G:\CG\3D\Common\Cube.obj").First();

        sceneObject = new GlRenderObject(cube, new Transform3D
        {
            Translation = Vector3D<float>.UnitX / 2,
            Scale = Vector3D<float>.One / 4,
            Rotation = new Vector3D<float>(Scalar.DegreesToRadians(-20), Scalar.DegreesToRadians(30), 0)
        }, defaultMaterial.MaterialId);
        anotherObject = new GlRenderObject(cube, new Transform3D
        {
            Translation = Vector3D<float>.UnitX / -2 + Vector3D<float>.UnitZ * -3,
            Scale = Vector3D<float>.One / 2
        }, defaultMaterial.MaterialId);

        viewer = new Viewer(new Vector2D<float>(window!.Size.X, window.Size.Y));
        viewer.Position = Vector3D<float>.UnitZ * -3;
        
        scene = new RenderScene(viewer);
        
        scene.RenderedObjects.Add(sceneObject);
        scene.RenderedObjects.Add(anotherObject);
        
        gl.ClearColor(Color.Black);
        gl.Enable(EnableCap.DepthTest);
        
        renderer!.Initialize(new GlSceneRenderContext(Enumerable.Empty<IShaderDefinition>(), scene, new []{defaultMaterial}), new GlRenderBackendProvider(gl));

        gui = new ImGuiController(gl, window, input);
        
        for (var i = 0; i < input.Keyboards.Count; i++)
        {
            input.Keyboards[i].KeyDown += OnKeyDown;
        }
    }
    
    private void OnUpdate(double delta)
    {
        if (input?.Keyboards[0] is { } k)
        {
            if (renderer != null)
            {
                const float movementDelta = 0.01f;
                
                var leftFactor = (k.IsKeyPressed(Key.A) ? 1 : 0) * movementDelta * Vector3D<float>.UnitX;
                var rightFactor = (k.IsKeyPressed(Key.D) ? 1 : 0) * movementDelta * -Vector3D<float>.UnitX;
                var forwardFactor = (k.IsKeyPressed(Key.W) ? 1 : 0) * movementDelta * Vector3D<float>.UnitZ;
                var backwardFactor = (k.IsKeyPressed(Key.S) ? 1 : 0) * movementDelta * -Vector3D<float>.UnitZ;
                var upFactor = (k.IsKeyPressed(Key.E) ? 1 : 0) * movementDelta * Vector3D<float>.UnitY;
                var downFactor = (k.IsKeyPressed(Key.Q) ? 1 : 0) * movementDelta * -Vector3D<float>.UnitY;

                viewer!.Position += leftFactor + rightFactor + forwardFactor + backwardFactor + upFactor + downFactor;
            }
        }
        
        gui?.Update((float)delta);
        viewer!.IsOrthographic = isOrthographic;
        viewer.FieldOfView = fov;
    }
    
    private void OnFrameBufferResize(Vector2D<int> size)
    {
        gl?.Viewport(0, 0, (uint)size.X, (uint)size.Y);

        if (viewer != null)
        {
            viewer.ScreenSize = new Vector2D<float>(size.X, size.Y);
        }
    }
    
    private void OnRender(double obj)
    {
        renderer!.RenderFrame();
        
        PrepareGui();
        gui?.Render();
    }

    private void PrepareGui()
    {
        ImGui.Begin("Debug");
        
        ImGui.SeparatorText("Info");
        
        ImGui.BeginGroup();
        ImGui.Text($"FPS: {window?.FramesPerSecond}");
        ImGui.Text($"Time: {window?.Time}");
        ImGui.EndGroup();
        
        ImGui.SeparatorText("Render");

        ImGui.BeginGroup();
        ImGui.SliderFloat("Time Mult", ref timeMultiplier, 0, 10);
        if (ImGui.Button("Reload Shaders"))
        {
            renderer?.InjectMaterial(defaultMaterial!);
        }
        ImGui.EndGroup();
        
        ImGui.SeparatorText("View");

        ImGui.BeginGroup();
        ImGui.Checkbox("Orthographic", ref isOrthographic);
        ImGui.SliderFloat("FOV", ref fov, 10, 100);
        ImGui.EndGroup();
        
        ImGui.End();
    }

    private IGlMaterialDefinition GetSampleMaterial()
    {
        var uniforms = new IGlShaderUniform[]
        {
            new GlFloatUniform(UniformConvention.TimeUniformName, () => (float)(window?.Time ?? 0) * timeMultiplier, true),
            new GlIntUniform("mainTexture", () => 0),
            new GlIntUniform("distortion", () => 1)
        };
        
        var shaders = new IShaderDefinition[]
        {
            new ShaderDefinition(new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Common\Nodus.RenderEngine.Avalonia\Assets\Shaders\example.frag"), ShaderSourceType.Fragment),
            new ShaderDefinition(new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\standard.vert"), ShaderSourceType.Vertex)
        };
        
        return new GlMaterialDefinition(shaders, uniforms, textures!);
    }

    private IGlMaterialDefinition GetFallbackMaterial()
    {
        var shaders = new IShaderDefinition[]
        {
            new ShaderDefinition(new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\fallback.frag"), ShaderSourceType.Fragment),
            new ShaderDefinition(new ShaderFileSource(@"C:\Users\Donatoz\RiderProjects\Nodus\Nodus.VisualTests\Assets\Shaders\standard.vert"), ShaderSourceType.Vertex)
        };

        return new GlMaterialDefinition(shaders, Enumerable.Empty<IGlShaderUniform>(), Enumerable.Empty<IGlTexture>());
    }

    private bool switcher;
    
    private void OnKeyDown(IKeyboard keyboard, Key key, int n)
    {
        if (key == Key.Tab)
        {
            switcher = !switcher;
            anotherObject!.IsRendered = switcher;
        }
        
        if (key == Key.Escape)
        {
            Exit();
        }
    }

    private void Exit()
    {
        renderer.Dispose();
        gui?.Dispose();
        input?.Dispose();
        textures?.DisposeAll();

        gl?.Dispose();
        
        window.Close();
    }
}