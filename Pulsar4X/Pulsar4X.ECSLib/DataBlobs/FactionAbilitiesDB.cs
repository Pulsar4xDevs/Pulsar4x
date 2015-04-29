using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class FactionAbilitiesDB : BaseDataBlob
    {

        public float BaseConstructionBonus { get; set; }

        public float BaseFighterBonus { get; set; }

        public float BaseMiningBonus { get; set; }

        public float BaseOrdnanceBonus { get; set; }

        public float BaseResearchBonus { get; set; }

        public float BaseShipBuildingBonus { get; set; }

        public float BaseTerraformingBonus { get; set; }

        public int BasePlanetarySensorStrength { get; set; }

        public float BaseGroundUnitStrengthBonus { get; set; }

        /// <summary>
        /// To determine final colony costs, from the Colonization Cost Reduction X% techs.
        /// </summary>
        public float ColonyCostMultiplier { get; set; }

        /// <summary>
        /// Default faction abilities constructor.
        /// </summary>
        public FactionAbilitiesDB()
            : this(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.001f, 250, 1.0f, 1.0f)
        {

        }

        public FactionAbilitiesDB(float baseConstructionBonus,
            float baseFighterBonus,
            float baseMiningBonus,
            float baseOrdnanceBonus,
            float baseResearchBonus,
            float baseShipBuildingBonus,
            float baseTerraformingBonus,
            int basePlanetarySensorStrength,
            float baseGroundUnitStrengthBonus,
            float colonyCostMultiplier)
        {
            BaseConstructionBonus = baseConstructionBonus;
            BaseFighterBonus = baseFighterBonus;
            BaseMiningBonus = baseMiningBonus;
            BaseOrdnanceBonus = baseOrdnanceBonus;
            BaseResearchBonus = baseResearchBonus;
            BaseShipBuildingBonus = baseShipBuildingBonus;
            BaseTerraformingBonus = baseTerraformingBonus;
            BasePlanetarySensorStrength = basePlanetarySensorStrength;
            BaseGroundUnitStrengthBonus = baseGroundUnitStrengthBonus;
            ColonyCostMultiplier = colonyCostMultiplier;
        }

        public FactionAbilitiesDB(FactionAbilitiesDB db)
        {
            BaseConstructionBonus = db.BaseConstructionBonus;
            BaseFighterBonus = db.BaseFighterBonus;
            BaseMiningBonus = db.BaseMiningBonus;
            BaseOrdnanceBonus = db.BaseOrdnanceBonus;
            BaseResearchBonus = db.BaseResearchBonus;
            BaseShipBuildingBonus = db.BaseShipBuildingBonus;
            BaseTerraformingBonus = db.BaseTerraformingBonus;
            BasePlanetarySensorStrength = db.BasePlanetarySensorStrength;
            BaseGroundUnitStrengthBonus = db.BaseGroundUnitStrengthBonus;
            ColonyCostMultiplier = db.ColonyCostMultiplier;
        }

        public override object Clone()
        {
            return new FactionAbilitiesDB(this);
        }
    }
}