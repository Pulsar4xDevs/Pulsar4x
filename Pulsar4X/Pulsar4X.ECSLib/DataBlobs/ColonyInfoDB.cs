using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class ColonyInfoDB : BaseDataBlob
    {
        public JDictionary<Entity, double> Population { get; set; }

        public JDictionary<Guid, int> MineralStockpile { get; set; }

        // JDictionary ShipComponentStockpile
        // JDictionary OrdananceStockpile
        // JDictionary FighterStockpile
        // JDictionary PDC_ComponentsStockpile

        public Entity PlanetEntity { get; set; }


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
            MineralStockpile = new JDictionary<Guid, int>();
            MineralStockpile = (JDictionary<Guid, int>)StaticDataManager.StaticDataStore.Minerals.ToDictionary(key => key.ID, val => 0);

        }

        public ColonyInfoDB(Entity species, double populationInMillions, Entity planet)
        {
            Population = new JDictionary<Entity, double> {{species, populationInMillions}};
            PlanetEntity = planet;
            MineralStockpile = new JDictionary<Guid, int>();
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
