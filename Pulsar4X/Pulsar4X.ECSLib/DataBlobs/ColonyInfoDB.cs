using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class ColonyInfoDB : BaseDataBlob
    {
        public JDictionary<SpeciesDB, double> Population;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="popSize">Species and population number(in Million?)</param>
        public ColonyInfoDB(JDictionary<SpeciesDB, double> popSize)
        {
            Population = popSize;
        }

        public ColonyInfoDB()
            : base()
        { }
    }
}
