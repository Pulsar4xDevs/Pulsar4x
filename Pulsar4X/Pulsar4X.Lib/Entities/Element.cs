using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.Entities
{
    public class Elemental
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public double AtomicWeight { get; set; }
        public double MeltingPoint { get; set; }
        public double BoilingPoint { get; set; }
        public double Density { get; set; }
        public double AbundanceE { get; set; }
        public double AbundanceS { get; set; }
        public double Reactivity { get; set; }
        public double MaximumInspiredPartialPressure { get; set; } //max inspired partial pressure (in mb)
    }
}
