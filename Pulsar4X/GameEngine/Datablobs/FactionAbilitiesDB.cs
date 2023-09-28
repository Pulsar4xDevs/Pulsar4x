using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;
using Pulsar4X.Extensions;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Datablobs
{
    public class FactionAbilitiesDB : BaseDataBlob, IGetValuesHash
    {
        public int BasePlanetarySensorStrength { get; set; }

        public float BaseGroundUnitStrengthBonus { get; set; }

        public Dictionary<AbilityType, float> AbilityBonuses { get; set; }

        /// <summary>
        /// To determine final colony costs, from the Colonization Cost Reduction X% techs.
        /// </summary>
        public float ColonyCostMultiplier { get; set; }

        /// <summary>
        /// Default faction abilities constructor.
        /// </summary>
        public FactionAbilitiesDB()
            : this(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.001f, 250, 1.0f, 1.0f)
        {

        }

        public FactionAbilitiesDB(float constructionBonus,
            float fighterConstructionBonus,
            float miningBonus,
            float refiningBonus,
            float ordnanceConstructionBonus,
            float researchBonus,
            float shipAsseblyBonus,
            float terraformingBonus,
            int basePlanetarySensorStrength,
            float groundUnitStrengthBonus,
            float colonyCostMultiplier)
        {

            BasePlanetarySensorStrength = basePlanetarySensorStrength;
            BaseGroundUnitStrengthBonus = groundUnitStrengthBonus;
            ColonyCostMultiplier = colonyCostMultiplier;

            AbilityBonuses = new Dictionary<AbilityType, float>();
            AbilityBonuses.Add(AbilityType.GenericConstruction, constructionBonus);
            AbilityBonuses.Add(AbilityType.FighterConstruction, fighterConstructionBonus);
            AbilityBonuses.Add(AbilityType.Mine, miningBonus);
            AbilityBonuses.Add(AbilityType.Refinery, refiningBonus);
            AbilityBonuses.Add(AbilityType.OrdnanceConstruction, ordnanceConstructionBonus);
            AbilityBonuses.Add(AbilityType.Research, researchBonus);
            AbilityBonuses.Add(AbilityType.ShipAssembly, shipAsseblyBonus);
            AbilityBonuses.Add(AbilityType.Terraforming, terraformingBonus);

        }

        public FactionAbilitiesDB(FactionAbilitiesDB db)
        {
            AbilityBonuses = new Dictionary<AbilityType, float>(db.AbilityBonuses);
            BasePlanetarySensorStrength = db.BasePlanetarySensorStrength;
            BaseGroundUnitStrengthBonus = db.BaseGroundUnitStrengthBonus;
            ColonyCostMultiplier = db.ColonyCostMultiplier;
        }

        public override object Clone()
        {
            return new FactionAbilitiesDB(this);
        }

        public int GetValueCompareHash(int hash = 17)
        {

            foreach (var item in AbilityBonuses)
            {
                hash = ObjectExtensions.ValueHash(item.Key, hash);
                hash = ObjectExtensions.ValueHash(item.Value, hash);
            }

            hash = ObjectExtensions.ValueHash(BasePlanetarySensorStrength, hash);
            hash = ObjectExtensions.ValueHash(BaseGroundUnitStrengthBonus, hash);
            hash = ObjectExtensions.ValueHash(ColonyCostMultiplier, hash);
            return hash;
        }

    }
}