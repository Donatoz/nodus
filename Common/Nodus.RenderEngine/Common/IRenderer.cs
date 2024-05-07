namespace Nodus.RenderEngine.Common;

public interface IRenderContext
{
}

public interface IRenderer : IRenderDispatcher
{
    void Initialize(IRenderContext context);
    void RenderFrame();
    void UpdateShaders(IEnumerable<IShaderDefinition> shaders);
}