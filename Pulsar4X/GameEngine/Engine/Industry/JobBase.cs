using System;
using System.Collections.Generic;

namespace Pulsar4X.Engine.Industry
{
    public abstract class JobBase
    {
        public virtual string Name { get; internal set; }
        public Guid JobID = Guid.NewGuid();
        public string ItemGuid { get; protected set; }
        public ushort NumberOrdered { get; set; }
        public ushort NumberCompleted { get; internal set; }

        /// <summary>
        /// for single item under construction.
        /// </summary>
        public long ProductionPointsLeft
        {
            get;
            internal set;
        }

        /// <summary>
        /// Per Item
        /// </summary>
        public long ProductionPointsCost { get; protected set; }
        public bool Auto { get; internal set; }

        public Dictionary<string, long> ResourcesRequiredRemaining { get; internal set; } = new ();
        public Dictionary<string, long> ResourcesCosts { get; internal set; } = new ();

        public JobBase()
        {
        }

        public JobBase(string guid, ushort numberOrderd, int jobPoints, bool auto)
        {
            ItemGuid = guid;
            NumberOrdered = numberOrderd;
            NumberCompleted = 0;
            ProductionPointsLeft = jobPoints;
            ProductionPointsCost = jobPoints;
            Auto = auto;
        }

        public abstract void InitialiseJob(ushort numberOrderd, bool auto);

    }
}