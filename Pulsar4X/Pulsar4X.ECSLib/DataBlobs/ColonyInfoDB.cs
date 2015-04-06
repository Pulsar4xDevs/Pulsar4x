using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.Helpers;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class ColonyInfoDB : BaseDataBlob
    {
        public JDictionary<DataBlobRef<SpeciesDB>, double> Population;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="popSize">Species and population number(in Million?)</param>
        public ColonyInfoDB(JDictionary<DataBlobRef<SpeciesDB>, double> popSize)
        {
            Population = popSize;
        }

        public ColonyInfoDB()
            : base()
        { }
    }
}
