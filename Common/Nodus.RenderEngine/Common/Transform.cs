using Silk.NET.Maths;

namespace Nodus.RenderEngine.Common;

public interface ITransform
{
    /// <summary>
    /// Get the transformation/model matrix.
    /// </summary>
    /// <returns></returns>
    Matrix4X4<float> GetMatrix();
}

public abstract class TransformBase : ITransform
{
    protected bool IsDirty { get; set; }

    private Matrix4X4<float>? lastCachedMatrix;
    
    public Matrix4X4<float> GetMatrix()
    {
        if (lastCachedMatrix == null || IsDirty)
        {
            lastCachedMatrix = CreateNewMatrix();
            IsDirty = false;
        }

        return lastCachedMatrix.Value;
    }

    protected abstract Matrix4X4<float> CreateNewMatrix();
}

public class Transform2D : TransformBase
{
    private Vector2D<float> translation;
    private float rotation;
    private Vector2D<float> scale;
    
    public Vector2D<float> Translation
    {
        get => translation;
        set
        {
            translation = value;
            IsDirty = true;
        }
    }

    /// <summary>
    /// A rotation along Z axis.
    /// </summary>
    public float Rotation
    {
        get => rotation;
        set
        {
            rotation = value;
            IsDirty = true;
        }
    }

    public Vector2D<float> Scale
    {
        get => scale;
        set
        {
            scale = value;
            IsDirty = true;
        }
    }

    public Transform2D()
    {
        Translation = Vector2D<float>.Zero;
        Rotation = 0f;
        Scale = Vector2D<float>.One;
    }
    
    protected override Matrix4X4<float> CreateNewMatrix()
    {
        return Matrix4X4.CreateTranslation(new Vector3D<float>(Translation.X, Translation.Y, 0)) *
               Matrix4X4.CreateRotationZ(Rotation) *
               Matrix4X4.CreateScale(new Vector3D<float>(Scale.X, Scale.Y, 0));
    }
}

public class Transform3D : TransformBase
{
    private Vector3D<float> translation;
    private Vector3D<float> rotation;
    private Vector3D<float> scale;

    public Vector3D<float> Translation
    {
        get => translation;
        set
        {
            translation = value;
            IsDirty = true;
        }
    }
    
    public Vector3D<float> Rotation
    {
        get => rotation;
        set
        {
            rotation = value;
            IsDirty = true;
        }
    }
    
    public Vector3D<float> Scale
    {
        get => scale;
        set
        {
            scale = value;
            IsDirty = true;
        }
    }

    public Transform3D()
    {
        Translation = Vector3D<float>.Zero;
        Rotation = Vector3D<float>.Zero;
        Scale = Vector3D<float>.One;
    }

    protected override Matrix4X4<float> CreateNewMatrix()
    {
        return Matrix4X4.CreateTranslation(Translation) *
               Matrix4X4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
               Matrix4X4.CreateScale(Scale);
    }
}