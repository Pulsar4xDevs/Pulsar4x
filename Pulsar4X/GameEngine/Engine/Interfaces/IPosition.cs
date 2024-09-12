using Pulsar4X.Orbital;

namespace Pulsar4X.Interfaces
{
    public interface IPosition
    {
        Vector3 AbsolutePosition { get; }
        Vector3 RelativePosition { get; }
    }
}