using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    [StaticData(true, IDPropertyName = "ResourceID")]
    public class ArmorSD
    {
        public Guid ResourceID { get; set; }
        public double Density { get; set; }
    }
}