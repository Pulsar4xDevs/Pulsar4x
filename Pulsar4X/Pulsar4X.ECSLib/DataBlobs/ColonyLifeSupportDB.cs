using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public class ColonyLifeSupportDB : BaseDataBlob
    {
        public long MaxPopulation { get; set; }

        public ColonyLifeSupportDB()
        {
            MaxPopulation = new long();
        }

        public ColonyLifeSupportDB(ColonyLifeSupportDB db)
        {
            MaxPopulation = db.MaxPopulation;
        }

        public override object Clone()
        {
            return new ColonyLifeSupportDB(this);
        }
    }
}
