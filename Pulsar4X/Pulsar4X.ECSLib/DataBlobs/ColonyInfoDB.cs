using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class ColonyInfoDB : BaseDataBlob
    {
        private Entity _factionEntity;

        public Entity FactionEntity
        {
            get
            {
                return _factionEntity;
            }
            internal set
            {
                if(value.HasDataBlob<FactionInfoDB>())
                    _factionEntity = value;
                else
                    throw new Exception("Entity Not a faction or does not contain a FactionInfoDB");                
            }
        }

        /// <summary>
        /// Species Entity and amount
        /// </summary>
        public JDictionary<Entity, long> Population
        {
            get { return _population; }
            internal set { _population = value; }
        }
        [JsonProperty]
        private JDictionary<Entity, long> _population;

        /// <summary>
        /// Raw Mined minerals. Mines push here, Refinary pulls from here, Construction pulls from here.
        /// </summary>
        public JDictionary<Guid, int> MineralStockpile
        {
            get { return _mineralStockpile; }
            internal set { _mineralStockpile = value; }
        }
        [JsonProperty]
        private JDictionary<Guid, int> _mineralStockpile;

        /// <summary>
        ///refined Fuel, or refined minerals if the modder so desires.
        /// Refinary pushes here, Construction pulls from here.
        /// </summary>
        public JDictionary<Guid, int> RefinedStockpile
        {
            get { return _refinedStockpile; }
            internal set { _refinedStockpile = value; }
        }
        [JsonProperty]
        private JDictionary<Guid, int> _refinedStockpile;

        /// <summary>
        /// constructed parts stockpile.
        /// Construction pulls and pushes from here.
        /// </summary>
        public JDictionary<Guid, int> ComponentStockpile
        {
            get { return _componentStockpile; }
            internal set { _componentStockpile = value; }
        }
        [JsonProperty]
        private JDictionary<Guid, int> _componentStockpile;

        /// <summary>
        /// Construction pushes here.
        /// </summary>
        public JDictionary<Guid, float> OrdinanceStockpile
        {
            get { return _ordinanceStockpile; }
            internal set { _ordinanceStockpile = value; }
        }
        [JsonProperty]
        private JDictionary<Guid, float> _ordinanceStockpile;
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


        public JDictionary<Entity, int> Installations { get;internal set; }


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
        public ColonyInfoDB(JDictionary<Entity, long> popCount, Entity planet, Entity faction)
        {
            FactionEntity = faction;
            Population = popCount;
            PlanetEntity = planet;
            
            MineralStockpile =  new JDictionary<Guid, int>();
            RefinedStockpile = new JDictionary<Guid, int>();
            ComponentStockpile = new JDictionary<Guid, int>();
            OrdinanceStockpile = new JDictionary<Guid, float>();
            FighterStockpile = new List<Entity>();
            Installations = new JDictionary<Entity, int>();
            Scientists = new List<Entity>();
        }

        public ColonyInfoDB(Entity species, long populationCount, Entity planet, Entity faction):this(
            new JDictionary<Entity, long> {{species, populationCount}},
            planet, faction
            )
        {
        }

        public ColonyInfoDB(ColonyInfoDB colonyInfoDB)
        {
            FactionEntity = colonyInfoDB.FactionEntity;
            Population = new JDictionary<Entity, long>(colonyInfoDB.Population);
            PlanetEntity = colonyInfoDB.PlanetEntity;
            MineralStockpile = new JDictionary<Guid, int>(colonyInfoDB.MineralStockpile);
            RefinedStockpile = new JDictionary<Guid, int>(colonyInfoDB.RefinedStockpile);
            ComponentStockpile = new JDictionary<Guid, int>(colonyInfoDB.ComponentStockpile);
            OrdinanceStockpile = new JDictionary<Guid, float>(colonyInfoDB.OrdinanceStockpile);
            FighterStockpile = new List<Entity>(colonyInfoDB.FighterStockpile);
            Installations = new JDictionary<Entity, int>(colonyInfoDB.Installations);
            Scientists = new List<Entity>(colonyInfoDB.Scientists);
        }

        public override object Clone()
        {
            return new ColonyInfoDB(this);
        }
    }
}
