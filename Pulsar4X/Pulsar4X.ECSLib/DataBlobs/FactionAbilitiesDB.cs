﻿using System.Collections.Generic;

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
        Agricultural, //as above.
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
        Terraforming,
        BasicLiving, //ie Auroras infrastructure will have this ability. 
        //shipcomponent
        ReducedSize,
        LaunchMissileSize,
        ReloadRateFromMag,
        ReloadRateFromHanger,
        ReloadRateMultiplyer,
        MissileMagazine,

        ComponentSize,
        EnginePower,
        EngineEfficency,
        FuelConsumption,
        ThermalSignature,
        EMSignature,

        Nothing

    }

    public class FactionAbilitiesDB : BaseDataBlob
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
                //OnPropertyChanged();
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
                //OnPropertyChanged();
            }
        }

        private float _baseGroundUnitStrengthBonus;

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
    }
}