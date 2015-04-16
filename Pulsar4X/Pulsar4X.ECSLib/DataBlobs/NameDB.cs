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
        public JDictionary<Entity, string> Name { get; set; }

        public NameDB()
        {
            Name = new JDictionary<Entity, string>();
        }

        /// <summary>
        /// Each faction can have a different name for whatever entity has this blob.
        /// </summary>
        /// <param name="primaryFaction">This entities faction</param>
        /// <param name="primaryName">The name this faction uses for this entity</param>
        public NameDB(Entity primaryFaction, string primaryName)
        {
            Name = new JDictionary<Entity, string>();
            Name.Add(primaryFaction, primaryName);
        }

        public NameDB(NameDB nameDB)
        {
            Name = new JDictionary<Entity, string>(nameDB.Name);
        }

        public override object Clone()
        {
            return new NameDB(this);
        }
    }
}
