using Pulsar4X.Orbital;

namespace Pulsar4X.Client.Rendering;

public class Camera2
{
    private const float MAX_ZOOMLEVEL = 1.496e+11f;
    private Vector2 _position;
    private float _zoom;
    private float _zoomSpeed = 1.25f;
    private float _rotation;
    private System.Numerics.Matrix4x4 _projectionMatrix;
    private Vector2 _screenSize;

    public Vector2 ScreenSize
    {
        get => _screenSize;
    }

    public Camera2(Vector2 screenSize)
    {
        _position = Vector2.Zero;
        _zoom = 200.0f;
        _rotation = 0.0f;
        _screenSize = screenSize;
        _projectionMatrix = CreateProjectionMatrix();
    }

    public void UpdateScreenSize(Vector2 screenSize)
    {
        _screenSize = screenSize;
        _projectionMatrix = CreateProjectionMatrix();
    }

    public Vector2 Position
    {
        get => _position;
        set => _position = value;
    }

    public float Zoom
    {
        get => _zoom;
    }

    public float Rotation
    {
        get => _rotation;
        set => _rotation = value;
    }

    public void Move(Vector2 amount)
    {
        _position += amount;
    }

    public void Rotate(float amount)
    {
        _rotation += amount;
    }

    public System.Numerics.Matrix4x4 GetViewMatrix()
    {
        return
            System.Numerics.Matrix4x4.CreateScale(_zoom, _zoom, 1.0f) *
            System.Numerics.Matrix4x4.CreateRotationZ(_rotation) *
            System.Numerics.Matrix4x4.CreateTranslation(new System.Numerics.Vector3((float)-_position.X, (float)-_position.Y, 0.0f));
    }

    public System.Numerics.Matrix4x4 GetViewProjectionMatrix()
    {
        return GetViewMatrix() * _projectionMatrix;
    }

    public float[] GetTransformMatrix()
    {
        var viewProjectionMatrix = GetViewProjectionMatrix();
        return new float[]
        {
            viewProjectionMatrix.M11, viewProjectionMatrix.M12, viewProjectionMatrix.M13, viewProjectionMatrix.M14,
            viewProjectionMatrix.M21, viewProjectionMatrix.M22, viewProjectionMatrix.M23, viewProjectionMatrix.M24,
            viewProjectionMatrix.M31, viewProjectionMatrix.M32, viewProjectionMatrix.M33, viewProjectionMatrix.M34,
            viewProjectionMatrix.M41, viewProjectionMatrix.M42, viewProjectionMatrix.M43, viewProjectionMatrix.M44
        };
    }

    private System.Numerics.Matrix4x4 CreateProjectionMatrix()
    {
        // Create orthographic projection matrix
        float left = 0;
        float right = (float)_screenSize.X;
        float bottom = (float)_screenSize.Y;
        float top = 0;
        return System.Numerics.Matrix4x4.CreateOrthographic(right - left, top - bottom, 0.1f, 100f);
    }

    public Vector2 ScaledPosition(Vector3 worldPositionInMeters)
    {
        return ScaledPosition(new Vector2(worldPositionInMeters.X, worldPositionInMeters.Y));
    }

    /// <summary>
    /// Scale a world position from meters to AU
    /// This is need since all the rendering is done in AU
    /// and all the world positions are in meters
    /// </summary>
    /// <param name="worldPositionInMeters"></param>
    /// <returns></returns>
    public Vector2 ScaledPosition(Vector2 worldPositionInMeters)
    {
        return new Vector2(
            Distance.MToAU(worldPositionInMeters.X),
            -Distance.MToAU(worldPositionInMeters.Y));
    }

    public void ZoomIn(int mouseX, int mouseY)
    {
        _zoom *= _zoomSpeed;
        if(_zoom > MAX_ZOOMLEVEL)
        {
            _zoom = MAX_ZOOMLEVEL;
        }

        // TODO: fix the zoom pan to the mouse coords
    }

    public void ZoomOut(int mouseX, int mouseY)
    {
        _zoom /= _zoomSpeed;
        if(_zoom < 0.1f)
        {
            _zoom = 0.1f;
        }

        // TODO: fix the zoom pan to the mouse coords
    }

    public float ViewDistance(double dist_AU)
    {
        return (float)(dist_AU * _zoom);
    }

    public Vector2[] Transform(Vector2[] points)
    {
        var matrix = GetViewMatrix();
        var results = new Vector2[points.Length];

        for(int i = 0; i < points.Length; i++)
        {
            results[i] = Transform(points[i], matrix);
        }

        return results;
    }

    public Vector2 Transform(Vector2 worldCoord_m, System.Numerics.Matrix4x4 transformBy)
    {
        // Scale from m to AU
        var scaled = ScaledPosition(worldCoord_m);

        // Convert to System.Numerics.Vector3 for transformation
        var pos3D = new System.Numerics.Vector3((float)scaled.X, (float)scaled.Y, 0f);

        // Transform through view matrix
        var transformed = System.Numerics.Vector3.Transform(pos3D, transformBy);

        // Convert back to Orbital.Vector2
        return new Vector2(transformed.X, transformed.Y);
    }

    public Vector2 WorldToScreenPosition(Vector2 worldCoord_m)
    {
        var screenCenter = ScreenSize / 2;
        var viewPosition = Transform(worldCoord_m, GetViewMatrix());

        return new Vector2(
            viewPosition.X + screenCenter.X,
            screenCenter.Y + viewPosition.Y
        );
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        var screenCenter = ScreenSize / 2;
        var centeredPosition = new Vector2(
            screenPosition.X - screenCenter.X,
            screenPosition.Y - screenCenter.Y
        );

        System.Numerics.Matrix4x4.Invert(GetViewMatrix(), out var inverseViewMatrix);
        var worldPosition = Transform(centeredPosition, inverseViewMatrix);

        return worldPosition;
    }
}