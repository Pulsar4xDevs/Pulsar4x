using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class FactionAbilitiesDB : BaseDataBlob
    {

        public int BaseConstructionRate { get; set; }

        public int BaseFighterRate { get; set; }

        public int BaseMiningRate { get; set; }

        public int BaseOrdnanceRate { get; set; }

        public int BaseResearchRate { get; set; }

        public int BaseShipBuildingRate { get; set; }

        public float BaseTerraformingRate { get; set; }

        public int BasePlanetarySensorStrength { get; set; }

        public int BaseGroundUnitStrength { get; set; }

        /// <summary>
        /// To determine final colony costs, from the Colonization Cost Reduction X% techs.
        /// </summary>
        public float ColonyCostMultiplier { get; set; }

        /// <summary>
        /// Default faction abilities constructor.
        /// </summary>
        public FactionAbilitiesDB()
            : this(10, 10, 10, 10, 200, 400, 0.001f, 250, 10, 1.0f)
        {

        }

        public FactionAbilitiesDB(int baseConstructionRate,
            int baseFighterRate,
            int baseMiningRate,
            int baseOrdnanceRate,
            int baseResearchRate,
            int baseShipBuildingRate,
            float baseTerraformingRate,
            int basePlanetarySensorStrength,
            int baseGroundUnitStrength,
            float colonyCostMultiplier)
        {
            BaseConstructionRate = baseConstructionRate;
            BaseFighterRate = baseFighterRate;
            BaseMiningRate = baseMiningRate;
            BaseOrdnanceRate = baseOrdnanceRate;
            BaseResearchRate = baseResearchRate;
            BaseShipBuildingRate = baseShipBuildingRate;
            BaseTerraformingRate = baseTerraformingRate;
            BasePlanetarySensorStrength = basePlanetarySensorStrength;
            BaseGroundUnitStrength = baseGroundUnitStrength;
            ColonyCostMultiplier = colonyCostMultiplier;
        }

        public FactionAbilitiesDB(FactionAbilitiesDB db)
        {
            BaseConstructionRate = db.BaseConstructionRate;
            BaseFighterRate = db.BaseFighterRate;
            BaseMiningRate = db.BaseMiningRate;
            BaseOrdnanceRate = db.BaseOrdnanceRate;
            BaseResearchRate = db.BaseResearchRate;
            BaseShipBuildingRate = db.BaseShipBuildingRate;
            BaseTerraformingRate = db.BaseTerraformingRate;
            BasePlanetarySensorStrength = db.BasePlanetarySensorStrength;
            BaseGroundUnitStrength = db.BaseGroundUnitStrength;
            ColonyCostMultiplier = db.ColonyCostMultiplier;
        }

        public override object Clone()
        {
            return new FactionAbilitiesDB(this);
        }
    }
}