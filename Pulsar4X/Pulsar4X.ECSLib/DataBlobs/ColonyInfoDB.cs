using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class ColonyInfoDB : BaseDataBlob
    {
        public JDictionary<Entity, long> Population { get; set; }

        /// <summary>
        /// Raw Mined minerals. Mines push here, Refinary pulls from here, Construction pulls from here.
        /// </summary>
        public JDictionary<Guid, float> MineralStockpile { get; set; } 
        /// <summary>
        /// Refinary pushes here, Construction pulls from here.
        /// </summary>
        public JDictionary<Guid, float> RefinedStockpile { get; set; } //refined Fuel, or refined minerals if the modder so desires.
        /// <summary>
        /// Construction pulls and pushes from here.
        /// </summary>
        public JDictionary<Guid, float> ComponentStockpile { get; set; } //constructed parts stockpile.
        /// <summary>
        /// Construction pushes here.
        /// </summary>
        public JDictionary<Guid, float> OrdananceStockpile { get; set; }
        /// <summary>
        /// Construction *adds* to this list. damaged and partialy constructed fighters will go here too, but shouldnt launch.
        /// </summary>
        public List<Entity> FighterStockpile { get; set; }

        public Entity PlanetEntity { get; set; }

        public List<Entity> Scientists; 

        public ColonyInfoDB()
        {
        }
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="popCount">Species and population number</param>
        /// <param name="planet"> the planet entity this colony is on</param>
        public ColonyInfoDB(JDictionary<Entity, long> popCount, Entity planet)
        {
            Population = popCount;
            PlanetEntity = planet;
            
            MineralStockpile =  new JDictionary<Guid, float>();
            RefinedStockpile = new JDictionary<Guid, float>();
            ComponentStockpile = new JDictionary<Guid, float>();
            OrdananceStockpile = new JDictionary<Guid, float>();
            FighterStockpile = new List<Entity>();
            Scientists = new List<Entity>();
        }

        public ColonyInfoDB(Entity species, long populationCount, Entity planet):this(
            new JDictionary<Entity, long> {{species, populationCount}},
            planet
            )
        {
        }

        public ColonyInfoDB(ColonyInfoDB colonyInfoDB)
        {
            Population = new JDictionary<Entity, long>(colonyInfoDB.Population);
            PlanetEntity = colonyInfoDB.PlanetEntity;
            MineralStockpile = new JDictionary<Guid, float>(colonyInfoDB.MineralStockpile);
            RefinedStockpile = new JDictionary<Guid, float>(colonyInfoDB.RefinedStockpile);
            ComponentStockpile = new JDictionary<Guid, float>(colonyInfoDB.ComponentStockpile);
            OrdananceStockpile = new JDictionary<Guid, float>(colonyInfoDB.OrdananceStockpile);
            FighterStockpile = new List<Entity>(colonyInfoDB.FighterStockpile);
            Scientists = new List<Entity>(colonyInfoDB.Scientists);
        }

        public override object Clone()
        {
            return new ColonyInfoDB(this);
        }
    }
}
