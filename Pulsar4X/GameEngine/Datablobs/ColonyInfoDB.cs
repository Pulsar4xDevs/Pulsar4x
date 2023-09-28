using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{
    public class ColonyInfoDB : BaseDataBlob
    {
        /// <summary>
        /// Species Entity and amount
        /// </summary>
        [JsonProperty]
        public Dictionary<Entity, long> Population { get; internal set; } = new ();


        /// <summary>
        /// constructed parts stockpile.
        /// Construction pulls and pushes from here.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, int> ComponentStockpile { get; internal set; } = new ();

        /// <summary>
        /// Construction pushes here.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, float> OrdinanceStockpile { get; internal set; } = new ();

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


        /// <summary>
        /// Installation list for damage calculations. Colony installations are considered components.
        /// </summary>
        public Dictionary<Entity, double> ColonyComponentDictionary { get; set; }

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

            ComponentStockpile = new Dictionary<string, int>();
            OrdinanceStockpile = new Dictionary<string, float>();
            FighterStockpile = new List<Entity>();
            ColonyComponentDictionary = new Dictionary<Entity, double>();
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
            ComponentStockpile = new Dictionary<string, int>(colonyInfoDB.ComponentStockpile);
            OrdinanceStockpile = new Dictionary<string, float>(colonyInfoDB.OrdinanceStockpile);
            FighterStockpile = new List<Entity>(colonyInfoDB.FighterStockpile);

            ColonyComponentDictionary = new Dictionary<Entity, double>(colonyInfoDB.ColonyComponentDictionary);
        }

        public override object Clone()
        {
            return new ColonyInfoDB(this);
        }
    }
}
