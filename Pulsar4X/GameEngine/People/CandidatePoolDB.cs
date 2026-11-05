using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;

namespace Pulsar4X.People
{
    /// <summary>
    /// One person in a colony's recruitment pool. The role is picked at hire: the same person can become an
    /// administrator or a lab technician.
    /// </summary>
    public class Candidate
    {
        public int Id { get; internal set; }
        public string Name { get; internal set; } = "";
        public int Age { get; internal set; }
        public int SpeciesId { get; internal set; } = -1;

        /// <summary>The tech category this person would favour as a lab technician.</summary>
        public string LabCategory { get; internal set; } = "";

        /// <summary>How many labs this person could run as a lab technician.</summary>
        public byte MaxLabs { get; internal set; }

        public DateTime JoinedOn { get; internal set; }
    }

    /// <summary>
    /// A colony's shortlist of people who could be hired. Refreshed on the colony's monthly population pass.
    /// A refresh never removes population. Only a hire does.
    /// </summary>
    public class CandidatePoolDB : BaseDataBlob
    {
        public List<Candidate> Candidates { get; internal set; } = new ();

        /// <summary>Unset until the first refresh. The next refresh is roughly a month later.</summary>
        public DateTime LastRefresh { get; internal set; }

        /// <summary>How many unpicked candidates the last refresh removed.</summary>
        public int LastRemoved { get; internal set; }

        /// <summary>How many new candidates the last refresh added.</summary>
        public int LastAdded { get; internal set; }

        public CandidatePoolDB() { }

        public CandidatePoolDB(CandidatePoolDB db)
        {
            Candidates = new List<Candidate>(db.Candidates);
            LastRefresh = db.LastRefresh;
            LastRemoved = db.LastRemoved;
            LastAdded = db.LastAdded;
        }

        public override object Clone() => new CandidatePoolDB(this);
    }
}
