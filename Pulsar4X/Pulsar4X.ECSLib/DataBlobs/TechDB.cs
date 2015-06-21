using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class TechDB : BaseDataBlob
    {
        [JsonProperty]
        private List<Guid> _researchedTechs;
        [JsonProperty]
        private Dictionary<TechSD, int> _researchableTechs;
        [JsonProperty]
        private List<TechSD> _unavailableTechs;
        [JsonProperty]
        private int _researchPoints;

        /// <summary>
        /// list of technolagies that have been fully researched.
        /// techs will be added to this list by the processor once research is complete.
        /// </summary>
        [PublicAPI]
        public List<Guid> ResearchedTechs
        {
            get { return _researchedTechs; }
            internal set { _researchedTechs = value; }
        }

        /// <summary>
        /// dictionary of technologies that are available to research, or are being researched. 
        /// techs will get added to this dict as they become available by the processor.
        /// </summary>
        [PublicAPI]
        public Dictionary<TechSD, int> ResearchableTechs
        {
            get { return _researchableTechs; }
            internal set { _researchableTechs = value; }
        }

        /// <summary>
        /// a list of techs not yet meeting the requirements to research
        /// </summary>
        [PublicAPI]
        public List<TechSD> UnavailableTechs
        {
            get { return _unavailableTechs; }
            internal set { _unavailableTechs = value; }
        }

        [PublicAPI]
        public int ResearchPoints
        {
            get { return _researchPoints; }
            internal set { _researchPoints = value; }
        }

        /// <summary>
        /// Constructor for datablob, this should only be used when a new faction is created.
        /// </summary>
        /// <param name="alltechs">a list of all possible techs in game</param>
        public TechDB(List<TechSD> alltechs)
        {
            UnavailableTechs = alltechs.ToList();
            ResearchedTechs = new List<Guid>();
            ResearchableTechs = new Dictionary<TechSD, int>();
            ResearchPoints = 0;
        }

        public TechDB(TechDB techDB)
        {
            UnavailableTechs = techDB.UnavailableTechs.ToList();
            ResearchedTechs = techDB.ResearchedTechs.ToList();
            ResearchableTechs = new Dictionary<TechSD, int>(techDB.ResearchableTechs);
            ResearchPoints = techDB.ResearchPoints;
        }

        public TechDB()
        {
            UnavailableTechs = new List<TechSD>();
            ResearchedTechs = new List<Guid>();
            ResearchableTechs = new Dictionary<TechSD, int>();
            ResearchPoints = 0;
        }

        public override object Clone()
        {
            return new TechDB(this);
        }
    }
}
