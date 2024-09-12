using System;
using Pulsar4X.Orbital;

namespace Pulsar4X.DataStructures
{
    public struct ManuverState
    {
        public DateTime At;
        public double Mass;
        public Vector3 Position;
        public Vector3 Velocity;
    }
}