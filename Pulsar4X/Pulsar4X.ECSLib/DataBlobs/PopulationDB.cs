using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.DataBlobs
{
    struct PopulationDB : IDataBlob
    {
        public int Entity { get { return m_entityID; } }
        private readonly int m_entityID;

        public readonly double PopulationSize;

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
