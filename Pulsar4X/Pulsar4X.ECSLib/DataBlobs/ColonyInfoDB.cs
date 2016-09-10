using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ColonyInfoDB : BaseDataBlob
    {
        /// <summary>
        /// Species Entity and amount
        /// </summary>
        [JsonProperty]
        public Dictionary<Entity, long> Population { get; internal set; } = new Dictionary<Entity, long>();

        /// <summary>
        /// Raw Mined minerals. Mines push here, Refinery pulls from here, Construction pulls from here.
        /// </summary>
        //[JsonProperty]
        //public Dictionary<Guid, int> MineralStockpile { get; internal set; } = new Dictionary<Guid, int>();

        /// <summary>
        ///refined Fuel, or refined minerals if the modder so desires.
        /// Refinery pushes here, Construction pulls from here.
        /// </summary>
        //[JsonProperty]
        //public Dictionary<Guid, int> RefinedStockpile { get; internal set; } = new Dictionary<Guid, int>();

        /// <summary>
        /// constructed parts stockpile.
        /// Construction pulls and pushes from here.
        /// </summary>
        [JsonProperty]
        public Dictionary<Guid, int> ComponentStockpile { get; internal set; } = new Dictionary<Guid, int>();

        /// <summary>
        /// Construction pushes here.
        /// </summary>
        [JsonProperty]
        public Dictionary<Guid, float> OrdinanceStockpile { get; internal set; } = new Dictionary<Guid, float>();

        /// <summary>
        /// Construction *adds* to this list. damaged and partialy constructed fighters will go here too, but shouldnt launch.
        /// </summary>
        [JsonProperty]
        public List<Entity> FighterStockpile { get; internal set; } = new List<Entity>();

        /// <summary>
        /// the parent planet
        /// </summary>
        [JsonProperty]
        public Entity PlanetEntity { get; internal set; } = Entity.InvalidEntity;

        [JsonProperty]
        public List<Entity> Scientists { get; internal set; } = new List<Entity>();

        public ColonyInfoDB() { }
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="popCount">Species and population number</param>
        /// <param name="planet"> the planet entity this colony is on</param>
        public ColonyInfoDB(Dictionary<Entity, long> popCount, Entity planet)
        {
            Population = popCount;
            PlanetEntity = planet;
            
            //MineralStockpile =  new Dictionary<Guid, int>();
            //RefinedStockpile = new Dictionary<Guid, int>();
            ComponentStockpile = new Dictionary<Guid, int>();
            OrdinanceStockpile = new Dictionary<Guid, float>();
            FighterStockpile = new List<Entity>();
            Scientists = new List<Entity>();
        }

        public ColonyInfoDB(Entity species, long populationCount, Entity planet):this(
            new Dictionary<Entity, long> {{species, populationCount}},
            planet)
        {
        }

        public ColonyInfoDB(ColonyInfoDB colonyInfoDB)
        {
            Population = new Dictionary<Entity, long>(colonyInfoDB.Population);
            PlanetEntity = colonyInfoDB.PlanetEntity;
            //MineralStockpile = new Dictionary<Guid, int>(colonyInfoDB.MineralStockpile);
            //RefinedStockpile = new Dictionary<Guid, int>(colonyInfoDB.RefinedStockpile);
            ComponentStockpile = new Dictionary<Guid, int>(colonyInfoDB.ComponentStockpile);
            OrdinanceStockpile = new Dictionary<Guid, float>(colonyInfoDB.OrdinanceStockpile);
            FighterStockpile = new List<Entity>(colonyInfoDB.FighterStockpile);            
            Scientists = new List<Entity>(colonyInfoDB.Scientists);
        }

        public override object Clone()
        {
            return new ColonyInfoDB(this);
        }
    }
}
