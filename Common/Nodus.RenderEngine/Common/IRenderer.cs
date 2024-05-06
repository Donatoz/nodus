namespace Nodus.RenderEngine.Common;

public interface IRenderer<in TCtx>
{
    void Initialize(TCtx context);
    void RenderFrame();
    void Enqueue(Action item);
    void UpdateShaders(IEnumerable<IShaderDefinition> shaders);
}