using System.Numerics;
using NUnit.Framework.Constraints;

namespace Pulsar4X.Helpers;

public static class NumericsVectorHelper
{
    public static Vector2 ToNumericsVector2(this Pulsar4X.Orbital.Vector3 source)
    {
        return new Vector2((float)source.X, (float)source.Y);
    }
    public static Vector3 ToNumericsVector3(this Pulsar4X.Orbital.Vector3 source)
    {
        return new Vector3((float)source.X, (float)source.Y, (float)source.Z);
    }
}