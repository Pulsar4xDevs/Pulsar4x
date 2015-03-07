using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4x.ECSLib.DataBlobs
{
    class OrbitDB : IDataBlob
    {
        public readonly double SemiMajorAxis;

        public OrbitDB(double semiMajorAxis)
        {
            SemiMajorAxis = semiMajorAxis;
        }



    }
}
