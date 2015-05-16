using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public enum AbilityType
    {
        ShipMaintenance,
        GenericConstruction, //ship parts, installations. 
        OrdnanceConstruction,
        FighterConstruction,
        ShipAssembly,
        Refinery,
        Mine,
        AtmosphericModification,
        Research,
        Commercial, //ie aurora "Finance Center" 
        Industrial, //intend to use this later on for civ economy and creating random tradegoods.
        Agrucultural, //as above.
        MassDriver,
        SpacePort, //loading/unloading speed;
        GeneratesNavalOfficers,
        GeneratesGroundOfficers,
        GeneratesShipCrew,
        GeneratesTroops, //not sure how we're going to do this yet.aurora kind of did toops and crew different.
        GeneratesScientists,
        GeneratesCivilianLeaders,
        DetectionThermal, //radar
        DetectionEM,    //radar
        Teraforming,
        BasicLiving //ie Auroras infrustructure will have this ability. 
    }

    public class FactionAbilitiesDB : BaseDataBlob, INotifyPropertyChanged
    {
        public int BasePlanetarySensorStrength
        {
            get { return _basePlanetarySensorStrength; }
            set
            {
                if (value == _basePlanetarySensorStrength)
                {
                    return;
                }
                _basePlanetarySensorStrength = value;
                OnPropertyChanged();
            }
        }

        private int _basePlanetarySensorStrength;

        public float BaseGroundUnitStrengthBonus
        {
            get { return _baseGroundUnitStrengthBonus; }
            set
            {
                if (value == _baseGroundUnitStrengthBonus)
                {
                    return;
                }
                _baseGroundUnitStrengthBonus = value;
                OnPropertyChanged();
            }
        }

        private float _baseGroundUnitStrengthBonus;

        public JDictionary<AbilityType, float> AbilityBonuses { get; set; }

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

            AbilityBonuses = new JDictionary<AbilityType, float>();
            AbilityBonuses.Add(AbilityType.GenericConstruction, constructionBonus);
            AbilityBonuses.Add(AbilityType.FighterConstruction, fighterConstructionBonus);
            AbilityBonuses.Add(AbilityType.Mine, miningBonus);
            AbilityBonuses.Add(AbilityType.Refinery, refiningBonus);
            AbilityBonuses.Add(AbilityType.OrdnanceConstruction, ordnanceConstructionBonus);
            AbilityBonuses.Add(AbilityType.Research, researchBonus);
            AbilityBonuses.Add(AbilityType.ShipAssembly, shipAsseblyBonus);
            AbilityBonuses.Add(AbilityType.Teraforming, terraformingBonus);

        }

        public FactionAbilitiesDB(FactionAbilitiesDB db)
        {
            AbilityBonuses = new JDictionary<AbilityType, float>(db.AbilityBonuses);
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