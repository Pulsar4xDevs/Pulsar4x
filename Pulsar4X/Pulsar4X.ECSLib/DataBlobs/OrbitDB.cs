using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4x.ECSLib.DataBlobs
{
    struct OrbitDB : IDataBlob
    {
        private readonly bool m_isValid;
        public bool isValid { get { return m_isValid; } }

        public readonly double SemiMajorAxis;

        public OrbitDB(double semiMajorAxis, bool p_isValid = true)
        {
            m_isValid = p_isValid;
            SemiMajorAxis = semiMajorAxis;
        }



    }
}
