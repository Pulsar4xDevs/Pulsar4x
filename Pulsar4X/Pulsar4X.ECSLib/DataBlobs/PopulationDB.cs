using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4x.ECSLib.DataBlobs
{
    struct PopulationDB : IDataBlob
    {
        private readonly bool m_isValid;
        public bool isValid { get { return m_isValid; } }

        public readonly double PopulationSize;

        public PopulationDB(double popSize, bool p_isValid = true)
        {
            m_isValid = p_isValid;
            PopulationSize = popSize;
        }
    }
}
