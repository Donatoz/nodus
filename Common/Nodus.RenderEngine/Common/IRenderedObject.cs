namespace Nodus.RenderEngine.Common;

public interface IRenderedObject
{
    string ObjectId { get; }
    IRenderedObject? Parent { get; }
    
    bool IsRendered { get; set; }
    IGeometryPrimitive Mesh { get; set; }
    ITransform Transform { get; set; }
    string MaterialId { get; set; }
}

public interface IRenderScene
{
    IList<IRenderedObject> RenderedObjects { get; }
    IViewer? Viewer { get; set; }
}

public class RenderedObject : IRenderedObject
{
    public string ObjectId { get; }
    public IRenderedObject? Parent { get; }
    
    public bool IsRendered { get; set; }
    public IGeometryPrimitive Mesh { get; set; }
    public ITransform Transform { get; set; }
    public string MaterialId { get; set; }
    
    public RenderedObject(IGeometryPrimitive geometry, ITransform transform, string materialId)
    {
        ObjectId = Guid.NewGuid().ToString();
        IsRendered = true;
        Mesh = geometry;
        MaterialId = materialId;
        Transform = transform;
    }
}

public class RenderScene : IRenderScene
{
    public IList<IRenderedObject> RenderedObjects { get; }
    public IViewer? Viewer { get; set; }

    public RenderScene(IViewer viewer)
    {
        RenderedObjects = new List<IRenderedObject>();
        Viewer = viewer;
    }
}