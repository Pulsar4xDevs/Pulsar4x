using System;
using System.Collections.Generic;
using Pulsar4X.Modding;

namespace Pulsar4X.ECSLib
{
    [StaticData(true, IDPropertyName = "ResourceID")]
    public class ArmorSD : SerializableGameData
    {
        /// <summary>
        /// this should correlate to a mineral or refined material. 
        /// </summary>
        public Guid ResourceID { get; set; } = Guid.Empty;
        /// <summary>
        /// this is for damage calculation. don't use this for mass or volume calcs. look up the resourceID instead. 
        /// </summary>
        public double Density { get; set; }
    }
}