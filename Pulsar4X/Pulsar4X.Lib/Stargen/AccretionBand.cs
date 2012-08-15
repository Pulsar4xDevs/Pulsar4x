using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Stargen
{
    public class AccretionBand
    {
        public double InnerEdge { get; set; }
        public double OuterEdge { get; set; }
        public bool DustPresent { get; set; }
        public bool GasPresent { get; set; }
    }
}
