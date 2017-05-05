using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// ScientistDB defines an entity as being a scientist, capable of research.
    /// </summary>
    /// <remarks>
    /// TODO: Gameplay Review
    /// ScientistDB and CommanderDB should probably be merged. Surely we can figure out a way
    /// for a single DB to cover all Leader functions?
    /// </remarks>
    public class ScientistDB : BaseDataBlob
    {
        /// <summary>
        /// Bonuses that this scentist imparts.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public Dictionary<ResearchCategories, float> Bonuses { get; internal set; }

        /// <summary>
        /// Max number of labs this scientist can manage.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public byte MaxLabs { get; internal set; }

        /// <summary>
        /// Current number of labs assigned to this scientist.
        /// </summary>
        [JsonProperty]
        public byte AssignedLabs { get; internal set; }

        /// <summary>
        /// Queue of projects currently being worked on by this scientist.
        /// </summary>
        /// <remarks>
        /// TODO: Pre-release Review
        /// Why is ProjectQueue not a queue?
        /// </remarks>
        public List<Guid> ProjectQueue { get; internal set; }
//        public List<Guid> ProjectQueue { get; internal set; } 

        public ScientistDB() { }

        public ScientistDB(Dictionary<ResearchCategories,float> bonuses, byte maxLabs )
        {
            Bonuses = bonuses;
            MaxLabs = maxLabs;
            AssignedLabs = 0;
            ProjectQueue = new List<Guid>();
        }

        public ScientistDB(ScientistDB dB)
        {
            Bonuses = new Dictionary<ResearchCategories, float>(dB.Bonuses);
            MaxLabs = dB.MaxLabs;
            AssignedLabs = dB.AssignedLabs;
            ProjectQueue = dB.ProjectQueue;
        }

        public override object Clone()
        {
            return new ScientistDB(this);
        }
    }
}