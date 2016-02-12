using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ColonyInfoDB : BaseDataBlob
    {
        private Entity _factionEntity;

        [JsonProperty]
        public Entity FactionEntity { get; internal set; }

        /// <summary>
        /// Species Entity and amount
        /// </summary>
        public Dictionary<Entity, long> Population
        {
            get { return _population; }
            internal set { _population = value; }
        }
        [JsonProperty]
        private Dictionary<Entity, long> _population;

        /// <summary>
        /// Raw Mined minerals. Mines push here, Refinary pulls from here, Construction pulls from here.
        /// </summary>
        public Dictionary<Guid, int> MineralStockpile
        {
            get { return _mineralStockpile; }
            internal set { _mineralStockpile = value; }
        }
        [JsonProperty]
        private Dictionary<Guid, int> _mineralStockpile;

        /// <summary>
        ///refined Fuel, or refined minerals if the modder so desires.
        /// Refinary pushes here, Construction pulls from here.
        /// </summary>
        public Dictionary<Guid, int> RefinedStockpile
        {
            get { return _refinedStockpile; }
            internal set { _refinedStockpile = value; }
        }
        [JsonProperty]
        private Dictionary<Guid, int> _refinedStockpile;

        /// <summary>
        /// constructed parts stockpile.
        /// Construction pulls and pushes from here.
        /// </summary>
        public Dictionary<Guid, int> ComponentStockpile
        {
            get { return _componentStockpile; }
            internal set { _componentStockpile = value; }
        }
        [JsonProperty]
        private Dictionary<Guid, int> _componentStockpile;

        /// <summary>
        /// Construction pushes here.
        /// </summary>
        public Dictionary<Guid, float> OrdinanceStockpile
        {
            get { return _ordinanceStockpile; }
            internal set { _ordinanceStockpile = value; }
        }
        [JsonProperty]
        private Dictionary<Guid, float> _ordinanceStockpile;
        /// <summary>
        /// Construction *adds* to this list. damaged and partialy constructed fighters will go here too, but shouldnt launch.
        /// </summary>
        public List<Entity> FighterStockpile
        {
            get { return _fighterStockpile; }
            internal set { _fighterStockpile = value; }
        }
        [JsonProperty]
        private List<Entity> _fighterStockpile;


        public Dictionary<Entity, int> Installations { get;internal set; }


        public Entity PlanetEntity
        {
            get { return _planetEntity; }
            internal set { _planetEntity = value; }
        }
        [JsonProperty]
        private Entity _planetEntity;

        public List<Entity> Scientists
        {
            get { return _scientists; }
            internal set { _scientists = value; }
        }
        [JsonProperty]
        private List<Entity> _scientists;

        public ColonyInfoDB()
        {
        }
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="popCount">Species and population number</param>
        /// <param name="planet"> the planet entity this colony is on</param>
        public ColonyInfoDB(Dictionary<Entity, long> popCount, Entity planet, Entity faction)
        {
            FactionEntity = faction;
            Population = popCount;
            PlanetEntity = planet;
            
            MineralStockpile =  new Dictionary<Guid, int>();
            RefinedStockpile = new Dictionary<Guid, int>();
            ComponentStockpile = new Dictionary<Guid, int>();
            OrdinanceStockpile = new Dictionary<Guid, float>();
            FighterStockpile = new List<Entity>();
            Installations = new Dictionary<Entity, int>();
            Scientists = new List<Entity>();
        }

        public ColonyInfoDB(Entity species, long populationCount, Entity planet, Entity faction):this(
            new Dictionary<Entity, long> {{species, populationCount}},
            planet, faction
            )
        {
        }

        public ColonyInfoDB(ColonyInfoDB colonyInfoDB)
        {
            FactionEntity = colonyInfoDB.FactionEntity;
            Population = new Dictionary<Entity, long>(colonyInfoDB.Population);
            PlanetEntity = colonyInfoDB.PlanetEntity;
            MineralStockpile = new Dictionary<Guid, int>(colonyInfoDB.MineralStockpile);
            RefinedStockpile = new Dictionary<Guid, int>(colonyInfoDB.RefinedStockpile);
            ComponentStockpile = new Dictionary<Guid, int>(colonyInfoDB.ComponentStockpile);
            OrdinanceStockpile = new Dictionary<Guid, float>(colonyInfoDB.OrdinanceStockpile);
            FighterStockpile = new List<Entity>(colonyInfoDB.FighterStockpile);
            Installations = new Dictionary<Entity, int>(colonyInfoDB.Installations);
            Scientists = new List<Entity>(colonyInfoDB.Scientists);
        }

        public override object Clone()
        {
            return new ColonyInfoDB(this);
        }
    }
}
