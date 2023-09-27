using Newtonsoft.Json;
using System.Collections.Generic;
using NCalc;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;

namespace Pulsar4X.Datablobs
{
    public class FactionTechDB : BaseDataBlob, IGetValuesHash
    {
        private FactionDataStore _factionData;

        [PublicAPI]
        [JsonProperty]
        public int ResearchPoints { get; internal set; }

        public List<(Scientist scientist, Entity atEntity)> AllScientists { get; internal set; } = new List<(Scientist, Entity)>();

        public bool IsResearchable(string id)
        {
            return _factionData.Techs.ContainsKey(id)
                    && _factionData.Techs[id].Level < _factionData.Techs[id].MaxLevel;
        }

        internal void IncrementLevel(string id)
        {
            if(!_factionData.Techs.ContainsKey(id)) return;

            var tech = _factionData.Techs[id];

            tech.Level++;
            tech.ResearchProgress = 0;
            tech.ResearchCost = TechCostFormula(tech);

            if(tech.Unlocks.ContainsKey(tech.Level))
            {
                foreach(var item in tech.Unlocks[tech.Level])
                {
                    _factionData.Unlock(item);

                    if(_factionData.Techs.ContainsKey(item))
                    {
                        var unlockedTech = (Tech)_factionData.Techs[item];
                        unlockedTech.ResearchCost = TechCostFormula(unlockedTech);
                    }
                }
            }
        }

        internal void AddPoints(string id, int pointsToAdd)
        {
            if(!_factionData.Techs.ContainsKey(id)) return;
            var tech = _factionData.Techs[id];

            var newPointsTotal = tech.ResearchProgress + pointsToAdd;
            if(newPointsTotal >= tech.ResearchCost)
            {
                int remainder = newPointsTotal - tech.ResearchCost;
                IncrementLevel(tech.UniqueID);
                tech.ResearchProgress = remainder;
            }
            else
            {
                tech.ResearchProgress = newPointsTotal;
            }
        }

        /// <summary>
        /// Constructor for datablob, this should only be used when a new faction is created.
        /// </summary>
        /// <param name="alltechs">a list of all possible techs in game</param>
        public FactionTechDB(FactionDataStore factionDataStore)
        {
            _factionData = factionDataStore;

            // Setup the initial research costs
            foreach(var (id, tech) in _factionData.LockedTechs)
            {
                tech.ResearchCost = TechCostFormula(tech);
            }

            foreach(var (id, tech) in _factionData.Techs)
            {
                tech.ResearchCost = TechCostFormula(tech);
            }

            ResearchPoints = 0;
        }

        public FactionTechDB(FactionTechDB techDB)
        {
            _factionData = techDB._factionData;
            ResearchPoints = techDB.ResearchPoints;
        }

        public override object Clone()
        {
            return new FactionTechDB(this);
        }

        public int GetValueCompareHash(int hash = 17)
        {
            hash = ObjectExtensions.ValueHash(ResearchPoints, hash);
            return hash;
        }

        public int TechCostFormula(Tech tech)
        {
            string stringExpression = tech.CostFormula;

            Expression expression = new Expression(stringExpression);
            expression.Parameters.Add("Level", tech.Level);
            int result = (int)expression.Evaluate();
            return result;
        }

        public double TechDataFormula(Tech tech)
        {
            string stringExpression = tech.DataFormula;

            Expression expression = new Expression(stringExpression);
            expression.Parameters.Add("Level", (double)tech.Level);
            object result = expression.Evaluate();
            if (result is int)
                return (double)(int)result;
            return (double)result;
        }
    }
}
