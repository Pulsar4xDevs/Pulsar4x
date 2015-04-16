using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// This datablob contains information on a ships ability to tractor other ships 
    /// and references to any ships it has tractored.
    /// </summary>
    public class TractorDB : BaseDataBlob
    {
        /// <summary>
        /// The number of tractors this ship has, which will determine how many ships it can tractor at once (1 ship per tractor)
        /// Will be 0 by default.
        /// </summary>
        public int NoOfTractors { get; set; }

        public List<Guid> TractoredShips { get; set; }

        public TractorDB() : this(0)
        {

        }

        public TractorDB(int noOfTractors)
        {
            NoOfTractors = noOfTractors;
            TractoredShips = new List<Guid>(NoOfTractors);
        }

        public TractorDB(TractorDB tractorDB)
        {
            NoOfTractors = tractorDB.NoOfTractors;
            TractoredShips = new List<Guid>(NoOfTractors); //Even if we copy datablob we shouldn't copy list of tractored ship
        }

        public override object Clone()
        {
            return new TractorDB(this);
        }
    }
}