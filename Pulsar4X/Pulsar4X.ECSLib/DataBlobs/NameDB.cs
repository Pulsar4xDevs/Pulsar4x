using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public class NameDB : BaseDataBlob
    {
        /// <summary>
        /// Each faction can have a different name for whatever entity has this blob.
        /// </summary>
        public Dictionary<Entity, string> Name { get; set; }

        /// <summary>
        /// Each faction can have a different name for whatever entity has this blob.
        /// </summary>
        /// <param name="primaryFaction">This entities faction</param>
        /// <param name="primaryName">The name this faction uses for this entity</param>
        public NameDB(Entity primaryFaction, string primaryName)
        {
            Name = new Dictionary<Entity, string>();
            Name.Add(primaryFaction, primaryName);
        }

        public NameDB(NameDB nameDB)
        {
            Name = nameDB.Name.ToDictionary(entry => entry.Key, entry => entry.Value);
        }
    }
}
