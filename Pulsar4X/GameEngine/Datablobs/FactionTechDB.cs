using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using NCalc;
using Pulsar4X.Blueprints;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;

namespace Pulsar4X.Datablobs
{
    public class FactionTechDB : BaseDataBlob, IGetValuesHash
    {
        /// <summary>
        /// dictionary of technolagy levels that have been fully researched.
        /// techs will be added to this dictionary or incremeted by the processor once research is complete.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public Dictionary<string, int> TechLevels { get; internal set; }

        private Dictionary<string, (TechBlueprint tech ,int pointsResearched, int pointCost)> Researchables = new ();

        public (TechBlueprint tech, int pointsResearched, int pointCost) GetResarchableTech(string id)
        {
            return Researchables[id];
        }

        public List<(TechBlueprint tech, int pointsResearched, int pointCost)> GetResearchableTechs()
        {
            return Researchables.Values.ToList();
        }

        public Dictionary<string, (TechBlueprint tech, int pointsResearched, int pointCost)> GetResearchablesDic()
        {
            return new Dictionary<string, (TechBlueprint tech, int pointsResearched, int pointCost)>(Researchables);
        }

        public bool IsResearchable(string id)
        {
            return Researchables.ContainsKey(id);
        }

        internal void IncrementLevel(string id)
        {

            TechBlueprint tech = Researchables[id].tech;
            if (TechLevels.ContainsKey(tech.UniqueID))
                TechLevels[tech.UniqueID] += 1;
            else
                TechLevels.Add(tech.UniqueID, 1);

            if (GetLevelforTech(tech) >= tech.MaxLevel)
            {
                Researchables.Remove(tech.UniqueID);
            }
            else
            {
                int newLevelCost = TechCostFormula(tech);
                Researchables[id] = (Researchables[id].tech, 0, newLevelCost);
            }
        }

        internal void AddPoints(string id, int pointsToAdd)
        {
            lock (Researchables) //because different systems which are on seperate threads may interact with this.
            {
                TechBlueprint tech = Researchables[id].tech;
                int points = Researchables[id].pointsResearched + pointsToAdd;
                if (points >= Researchables[id].pointCost)
                {
                    int remainder = points - Researchables[id].pointCost;
                    if (TechLevels.ContainsKey(tech.UniqueID))
                        TechLevels[tech.UniqueID] += 1;
                    else
                        TechLevels.Add(tech.UniqueID, 1);

                    if (GetLevelforTech(tech) >= tech.MaxLevel)
                    {
                        Researchables.Remove(tech.UniqueID);
                    }
                    else
                    {
                        int newLevelCost = TechCostFormula(tech);
                        Researchables[id] = (Researchables[id].tech, remainder, newLevelCost);
                    }
                    //ResearchProcessor.CheckRequrements(this);
                }
                else
                {
                    Researchables[id] = (Researchables[id].tech, points, Researchables[id].pointCost);
                }
            }


        }

        public void MakeResearchable(TechBlueprint tech)
        {
            if(!Researchables.ContainsKey(tech.UniqueID))
            {
                int cost = TechCostFormula(tech);
                Researchables.Add(tech.UniqueID, (tech, 0, cost));
            }

            if (UnavailableTechs.Contains(tech))
                UnavailableTechs.Remove(tech);
        }

        /// <summary>
        /// a list of techs not yet meeting the requirements to research
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public HashSet<TechBlueprint> UnavailableTechs { get; internal set; } = new HashSet<TechBlueprint>();

        [PublicAPI]
        [JsonProperty]
        public int ResearchPoints { get; internal set; }

        /// <summary>
        /// Constructor for datablob, this should only be used when a new faction is created.
        /// </summary>
        /// <param name="alltechs">a list of all possible techs in game</param>
        public FactionTechDB(List<TechBlueprint> alltechs)
        {

            foreach (var techSD in alltechs)
            {
                UnavailableTechs.Add(techSD);
            }

            TechLevels = new Dictionary<string, int>();

            ResearchPoints = 0;
        }

        public FactionTechDB(FactionTechDB techDB)
        {
            UnavailableTechs = new HashSet<TechBlueprint>(techDB.UnavailableTechs);
            TechLevels = new Dictionary<string, int>(techDB.TechLevels);

            ResearchPoints = techDB.ResearchPoints;
        }

        public FactionTechDB()
        {
            TechLevels = new Dictionary<string, int>();
            ResearchPoints = 0;
        }

        /// <summary>
        /// returns the level that this faction has researched for a given TechSD
        /// </summary>
        /// <param name="techSD"></param>
        /// <returns></returns>
        [PublicAPI]
        public int GetLevelforTech(TechBlueprint techSD)
        {
            if (TechLevels.ContainsKey(techSD.UniqueID))
                return TechLevels[techSD.UniqueID];
            else
                return 0;
        }

        public override object Clone()
        {
            return new FactionTechDB(this);
        }

        public int GetValueCompareHash(int hash = 17)
        {
            hash = ObjectExtensions.ValueHash(ResearchPoints, hash);
            foreach (var item in TechLevels)
            {
                hash = ObjectExtensions.ValueHash(item.Key, hash);
                hash = ObjectExtensions.ValueHash(item.Value, hash);
            }

            return hash;
        }

        public int TechCostFormula(TechBlueprint tech)
        {
            string stringExpression = tech.CostFormula;

            Expression expression = new Expression(stringExpression);
            expression.Parameters.Add("Level", GetLevelforTech(tech));
            int result = (int)expression.Evaluate();
            return result;
        }

        public double TechDataFormula(TechBlueprint tech)
        {
            string stringExpression = tech.DataFormula;

            Expression expression = new Expression(stringExpression);
            expression.Parameters.Add("Level", (double)GetLevelforTech(tech));
            object result = expression.Evaluate();
            if (result is int)
                return (double)(int)result;
            return (double)result;
        }
    }
}
