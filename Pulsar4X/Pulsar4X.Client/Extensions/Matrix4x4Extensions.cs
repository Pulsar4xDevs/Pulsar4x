using System.Numerics;

namespace Pulsar4X.Client.Rendering;

public static class Matrix4x4Extensions
{
    public static Matrix4x4 CreateMirror(bool x, bool y)
    {
        // Start with scale factors of 1 (no scaling)
        float scaleX = x ? -1.0f : 1.0f;
        float scaleY = y ? -1.0f : 1.0f;

        // CreateScale takes (x, y, z) scale factors
        return Matrix4x4.CreateScale(scaleX, scaleY, 1.0f);
    }
}