using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FactionTechDB : BaseDataBlob
    {
        [JsonProperty]
        private Dictionary<Guid,int> _researchedTechs;
        [JsonProperty]
        private Dictionary<TechSD, int> _researchableTechs;
        [JsonProperty]
        private Dictionary<TechSD, int> _unavailableTechs;
        [JsonProperty]
        private int _researchPoints;

        /// <summary>
        /// dictionary of technolagy levels that have been fully researched.
        /// techs will be added to this dictionary or incremeted by the processor once research is complete.
        /// </summary>
        [PublicAPI]
        public Dictionary<Guid,int> ResearchedTechs
        {
            get { return _researchedTechs; }
            internal set { _researchedTechs = value; }
        }

        /// <summary>
        /// returns the level that this faction has researched for a given TechSD
        /// </summary>
        /// <param name="techSD"></param>
        /// <returns></returns>
        [PublicAPI]
        public int LevelforTech(TechSD techSD)
        {
            if(_researchedTechs.ContainsKey(techSD.ID))
                return _researchedTechs[techSD.ID];
            else
                return 0;
        }

        /// <summary>
        /// dictionary of technologies that are available to research, or are being researched. 
        /// techs will get added to this dict as they become available by the processor.
        /// the int is how much research has been compleated on this tech.
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
        public Dictionary<TechSD, int> UnavailableTechs
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
        public FactionTechDB(List<TechSD> alltechs)
        {
            UnavailableTechs = new Dictionary<TechSD, int>();
            foreach (var techSD in alltechs)
            {             
                UnavailableTechs.Add(techSD,0);
            }
            
            ResearchedTechs = new Dictionary<Guid, int>();
            ResearchableTechs = new Dictionary<TechSD, int>();
            ResearchPoints = 0;
        }

        public FactionTechDB(FactionTechDB techDB)
        {
            UnavailableTechs = new Dictionary<TechSD, int>(techDB.UnavailableTechs);
            ResearchedTechs = new Dictionary<Guid, int>(techDB.ResearchedTechs);
            ResearchableTechs = new Dictionary<TechSD, int>(techDB.ResearchableTechs);
            ResearchPoints = techDB.ResearchPoints;
        }

        public FactionTechDB()
        {
            UnavailableTechs = new Dictionary<TechSD, int>();
            ResearchedTechs = new Dictionary<Guid, int>();
            ResearchableTechs = new Dictionary<TechSD, int>();
            ResearchPoints = 0;
        }

        public override object Clone()
        {
            return new FactionTechDB(this);
        }
    }
}
