using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4x.ECSLib.DataBlobs
{
    class PopulationDB : IDataBlob
    {
        public readonly double PopulationSize;

        public PopulationDB(double popSize)
        {
            PopulationSize = popSize;
        }
    }
}
