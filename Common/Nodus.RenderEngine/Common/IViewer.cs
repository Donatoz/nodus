using Silk.NET.Maths;

namespace Nodus.RenderEngine.Common;

/// <summary>
/// Represents a viewer in a render engine which serves as rendering pivot object.
/// </summary>
public interface IViewer
{
    /// <summary>
    /// Represents the position of a viewer in 3D space.
    /// </summary>
    Vector3D<float> Position { get; set; }

    Matrix4X4<float> GetView();
    Matrix4X4<float> GetProjection();
}

public interface IScreenViewer : IViewer
{
    Vector2D<float> ScreenSize { get; set; }
    public bool IsOrthographic { get; set; }
    public float FieldOfView { get; set; }
}

public class Viewer : IScreenViewer
{
    public Vector3D<float> Position { get; set; }
    public Vector2D<float> ScreenSize { get; set; }
    public bool IsOrthographic { get; set; }
    public float FieldOfView { get; set; }
    public float NearPlane { get; set; }
    public float FarPlane { get; set; }

    public Viewer(Vector2D<float> screenSize, float nearPlane = 0.1f, float farPlane = 100f, float fov = 45, Vector3D<float>? initialPosition = null, bool isOrthographic = false)
    {
        ScreenSize = screenSize;
        FieldOfView = fov;
        NearPlane = nearPlane;
        FarPlane = farPlane;

        Position = initialPosition ?? Vector3D<float>.Zero;
        IsOrthographic = isOrthographic;
    }

    public Matrix4X4<float> GetView()
    {
        return Matrix4X4.CreateTranslation(Position);
    }

    public Matrix4X4<float> GetProjection()
    {
        return IsOrthographic
            ? Matrix4X4.CreateOrthographic(ScreenSize.X, ScreenSize.Y, NearPlane, FarPlane)
            : Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(FieldOfView), ScreenSize.X / ScreenSize.Y, NearPlane, FarPlane);
    }

}