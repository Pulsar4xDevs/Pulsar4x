using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class PopulationDB : BaseDataBlob
    {
        public JDictionary<SpeciesDB, double> Population;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="popSize">Species and population number(in Million?)</param>
        public PopulationDB(JDictionary<SpeciesDB, double> popSize)
        {
            Population = popSize;
        }

        public PopulationDB()
            : base()
        { }
    }
}
