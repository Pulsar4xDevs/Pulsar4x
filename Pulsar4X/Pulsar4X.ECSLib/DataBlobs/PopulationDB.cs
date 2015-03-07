using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.DataBlobs
{
    class PopulationDB : IDataBlob
    {
        public int Entity { get { return m_entityID; } }
        private int m_entityID;

        public double PopulationSize;

        public PopulationDB(int entityID, double popSize)
        {
            m_entityID = entityID;
            PopulationSize = popSize;
        }

        public IDataBlob UpdateEntityID(int newEntityID)
        {
            return new PopulationDB(newEntityID, PopulationSize);
        }
    }
}
