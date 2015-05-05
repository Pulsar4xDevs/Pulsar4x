using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class ColonyInfoDB : BaseDataBlob
    {
        public JDictionary<Entity, double> Population { get; set; }

        /// <summary>
        /// Raw Mined minerals. Mines push here, Refinary pulls from here, Construction pulls from here.
        /// </summary>
        public JDictionary<Guid, int> MineralStockpile { get; set; } 
        /// <summary>
        /// Refinary pushes here, Construction pulls from here.
        /// </summary>
        public JDictionary<Guid, int> RefinedStockpile { get; set; } //refined Fuel, or refined minerals if the modder so desires.
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
        /// <param name="popSize">Species and population number(in Million?)</param>
        /// <param name="planet"> the planet entity this colony is on</param>
        public ColonyInfoDB(JDictionary<Entity, double> popSize, Entity planet)
        {
            Population = popSize;
            PlanetEntity = planet;
            
            MineralStockpile =  new JDictionary<Guid, int>();
            foreach (var mineral in StaticDataManager.StaticDataStore.Minerals)
            {
                MineralStockpile.Add(mineral.ID,0);
            }
            //MineralStockpile = new JDictionary<Guid, int>(StaticDataManager.StaticDataStore.Minerals.ToDictionary(key => key.ID, val => 0));
            
        }

        public ColonyInfoDB(Entity species, double populationInMillions, Entity planet)
        {
            Population = new JDictionary<Entity, double> {{species, populationInMillions}};
            PlanetEntity = planet;
            MineralStockpile = new JDictionary<Guid, int>(StaticDataManager.StaticDataStore.Minerals.ToDictionary(key => key.ID, val => 0));
        }

        public ColonyInfoDB(ColonyInfoDB colonyInfoDB)
        {
            Population = new JDictionary<Entity, double>(colonyInfoDB.Population);
            PlanetEntity = colonyInfoDB.PlanetEntity;
            MineralStockpile = new JDictionary<Guid, int>(colonyInfoDB.MineralStockpile);
        }

        public override object Clone()
        {
            return new ColonyInfoDB(this);
        }
    }
}
