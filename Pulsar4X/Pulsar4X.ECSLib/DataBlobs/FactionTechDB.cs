using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class FactionTechDB : BaseDataBlob
    {
        /// <summary>
        /// dictionary of technolagy levels that have been fully researched.
        /// techs will be added to this dictionary or incremeted by the processor once research is complete.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public Dictionary<Guid,int> ResearchedTechs { get; internal set; }

        /// <summary>
        /// dictionary of technologies that are available to research, or are being researched. 
        /// techs will get added to this dict as they become available by the processor.
        /// the int is how much research has been compleated on this tech.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public Dictionary<TechSD, int> ResearchableTechs { get; internal set; }

        /// <summary>
        /// a list of techs not yet meeting the requirements to research
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public Dictionary<TechSD, int> UnavailableTechs { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public int ResearchPoints { get; internal set; }

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

        /// <summary>
        /// returns the level that this faction has researched for a given TechSD
        /// </summary>
        /// <param name="techSD"></param>
        /// <returns></returns>
        [PublicAPI]
        public int LevelforTech(TechSD techSD)
        {
            if (ResearchedTechs.ContainsKey(techSD.ID))
                return ResearchedTechs[techSD.ID];
            else
                return 0;
        }

        public override object Clone()
        {
            return new FactionTechDB(this);
        }
    }
}
