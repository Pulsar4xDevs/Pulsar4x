using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Stargen
{
    public class AccreteBand
    {
        public double InnerEdge { get; set; }
        public double OuterEdge { get; set; }
        public bool DustPresent { get; set; }
        public bool GasPresent { get; set; }

        public bool Intersect(double inner, double outer)
        {
            if (OuterEdge <= inner) return false;
            if (InnerEdge >= outer) return false;
            return true;
        }

        public bool Intersect(ProtoPlanet p)
        {
            return Intersect(p.InnerEffectLimit, p.OuterEffectLimit);
        }

    }
}
