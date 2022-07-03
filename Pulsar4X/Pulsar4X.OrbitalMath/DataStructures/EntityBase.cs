using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.Orbital
{
    public class EntityBase
    {
        public bool IsStationary { get; set; }

        public KeplerElements Orbit { get; set; }

        public double DryMassInKG { get; set; }

        public Vector3 PositionInMetres { get; set; }

        public EntityBase Parent { get; set; }
    }
}
