using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Pulsar4X.ECSLib
{
    public class FactionTechDB : BaseDataBlob, IGetValuesHash
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
        //internal Dictionary<TechSD, int> ResearchableTechs { get; set; }
        
        private Dictionary<Guid, (TechSD tech ,int pointsResearched, int pointCost)> Researchables = new Dictionary<Guid, (TechSD, int, int)>();
        
        public (TechSD tech, int pointsResearched, int pointCost) GetResarchableTech(Guid id)
        {
            return Researchables[id];
        }

        public List<(TechSD tech, int pointsResearched, int pointCost)> GetResearchableTechs()
        {
            return Researchables.Values.ToList();
        }

        public Dictionary<Guid, (TechSD tech, int pointsResearched, int pointCost)> GetResearchablesDic()
        {
            return new Dictionary<Guid, (TechSD tech, int pointsResearched, int pointCost)>(Researchables);
        }

        public bool IsResearchable(Guid id)
        {
            return Researchables.ContainsKey(id);
        }

        internal void AddPoints(Guid id, int pointsToAdd)
        {
            lock (Researchables) //because different systems which are on seperate threads may interact with this.
            {
                TechSD tech = Researchables[id].tech;
                int points = Researchables[id].pointsResearched + pointsToAdd;
                if (points >= Researchables[id].pointCost)
                {
                    int remainder = points - Researchables[id].pointCost;
                    if (ResearchedTechs.ContainsKey(tech.ID))
                        ResearchedTechs[tech.ID] += 1;
                    else
                        ResearchedTechs.Add(tech.ID, 0);
                    
                    if (LevelforTech(tech) >= tech.MaxLevel)
                    {
                        Researchables.Remove(tech.ID);
                    }
                    else
                    {
                        int newLevelCost = ResearchProcessor.CostFormula(this, tech);
                        Researchables[id] = (Researchables[id].tech, remainder, newLevelCost);
                    }
                }
                else
                {
                    Researchables[id] = (Researchables[id].tech, points, Researchables[id].pointCost);
                }
            }
        }

        public void MakeResearchable(TechSD tech)
        {
            if(!Researchables.ContainsKey(tech.ID))
            {
                int cost = ResearchProcessor.CostFormula(this, tech);
                Researchables.Add(tech.ID, (tech, 0, cost));
            }

            if (UnavailableTechs.Contains(tech))
                UnavailableTechs.Remove(tech);
        }

        /// <summary>
        /// a list of techs not yet meeting the requirements to research
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public HashSet<TechSD> UnavailableTechs { get; internal set; } = new HashSet<TechSD>();

        [PublicAPI]
        [JsonProperty]
        public int ResearchPoints { get; internal set; }


        public List<(Scientist scientist, Entity atEntity)> AllScientists { get; internal set; } = new List<(Scientist, Entity)>();
        
        /// <summary>
        /// Constructor for datablob, this should only be used when a new faction is created.
        /// </summary>
        /// <param name="alltechs">a list of all possible techs in game</param>
        public FactionTechDB(List<TechSD> alltechs)
        {
            
            foreach (var techSD in alltechs)
            {             
                UnavailableTechs.Add(techSD);
            }
            
            ResearchedTechs = new Dictionary<Guid, int>();

            ResearchPoints = 0;
        }

        public FactionTechDB(FactionTechDB techDB)
        {
            UnavailableTechs = new HashSet<TechSD>(techDB.UnavailableTechs);
            ResearchedTechs = new Dictionary<Guid, int>(techDB.ResearchedTechs);

            ResearchPoints = techDB.ResearchPoints;
        }

        public FactionTechDB()
        {
            
            ResearchedTechs = new Dictionary<Guid, int>();

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

        public int GetValueCompareHash(int hash = 17)
        {
            hash = Misc.ValueHash(ResearchPoints, hash);
            foreach (var item in ResearchedTechs)
            {
                hash = Misc.ValueHash(item.Key, hash);  
                hash = Misc.ValueHash(item.Value, hash);
            } 

            return hash;
        }
    }
}
