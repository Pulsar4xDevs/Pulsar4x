using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Names;

namespace Pulsar4X.Colonies
{
    public class ColonyInfoDB : BaseDataBlob
    {
        /// <summary>
        /// Species Entity and amount
        /// </summary>
        public Dictionary<int, long> Population { get; internal set; } = new ();


        /// <summary>
        /// constructed parts stockpile.
        /// Construction pulls and pushes from here.
        /// </summary>
        public Dictionary<string, int> ComponentStockpile { get; internal set; } = new ();

        /// <summary>
        /// Construction pushes here.
        /// </summary>
        public Dictionary<string, float> OrdinanceStockpile { get; internal set; } = new ();

        /// <summary>
        /// Construction *adds* to this list. damaged and partialy constructed fighters will go here too, but shouldnt launch.
        /// </summary>
        public List<Entity> FighterStockpile { get; internal set; } = new List<Entity>();

        /// <summary>
        /// the parent planet
        /// </summary>
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
        public ColonyInfoDB(Dictionary<int, long> popCount, Entity planet)
        {
            Population = popCount;
            PlanetEntity = planet;

            ComponentStockpile = new Dictionary<string, int>();
            OrdinanceStockpile = new Dictionary<string, float>();
            FighterStockpile = new List<Entity>();
            ColonyComponentDictionary = new Dictionary<Entity, double>();
        }

        public ColonyInfoDB(Entity species, long populationCount, Entity planet):this(
            new Dictionary<int, long> {{species.Id, populationCount}},
            planet)
        {
        }

        public ColonyInfoDB(ColonyInfoDB colonyInfoDB)
        {
            Population = new Dictionary<int, long>(colonyInfoDB.Population);
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

        public new static List<Type> GetDependencies() => new List<Type>() { typeof(NameDB) };
    }
}
