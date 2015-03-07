using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.DataBlobs
{
    class PopulationDB : BaseDataBlob
    {
        public double PopulationSize;

        public PopulationDB(double popSize)
        {
            PopulationSize = popSize;
        }
    }
}
