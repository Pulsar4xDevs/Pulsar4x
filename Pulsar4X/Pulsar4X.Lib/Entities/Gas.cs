using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.Entities
{
    public class Gas
    {
        public int MoleculeId { get; set; }
        public double SurfacePressure { get; set; } //units of millibars (mb)
    }
}
