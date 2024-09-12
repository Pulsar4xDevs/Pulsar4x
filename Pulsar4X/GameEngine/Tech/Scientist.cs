using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine
{
    public class Scientist : TeamObject
    {

        /// <summary>
        /// Bonuses that this scentist imparts.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, float> Bonuses { get; internal set; }

        /// <summary>
        /// Max number of labs this scientist can manage.
        /// </summary>
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
        public SafeList<(string techID, bool cycle)> ProjectQueue { get; internal set; } = new ();

        public new string Name { get; set; }

        public Scientist()
        {
            TeamType = TeamTypes.Science;
        }

        public Scientist(Dictionary<string, float> bonuses, byte maxLabs )
        {
            Bonuses = bonuses;
            MaxLabs = maxLabs;
            AssignedLabs = 0;
            ProjectQueue = new ();
        }

        public Scientist(Scientist dB)
        {
            Bonuses = new Dictionary<string, float>(dB.Bonuses);
            MaxLabs = dB.MaxLabs;
            AssignedLabs = dB.AssignedLabs;
            ProjectQueue = dB.ProjectQueue;
        }

        public new object Clone()
        {
            return new Scientist(this);
        }
    }
}