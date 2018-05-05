using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class RenderingInfoDB : BaseDataBlob
    {
        Entity Faction { get; set; }
        List<OrbitRenderData> orbits = new List<OrbitRenderData>();
        public RenderingInfoDB()
        {
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

    struct OrbitRenderData
    {
        double X;
        double Y;
        double Z;

    }
}
