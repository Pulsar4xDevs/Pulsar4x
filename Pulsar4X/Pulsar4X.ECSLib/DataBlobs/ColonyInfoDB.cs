using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ColonyInfoDB : BaseDataBlob
    {
        public JDictionary<Entity, double> Population { get; set; }

        public JDictionary<MineralSD, int> MineralStockpile { get; set; }

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
            MineralStockpile = new JDictionary<MineralSD, int>();
        }

        public ColonyInfoDB(Entity species, double populationInMillions, Entity planet)
        {
            Population = new JDictionary<Entity, double> {{species, populationInMillions}};
            PlanetEntity = planet;
            MineralStockpile = new JDictionary<MineralSD, int>();
        }

        public ColonyInfoDB(ColonyInfoDB colonyInfoDB)
        {
            Population = new JDictionary<Entity, double>(colonyInfoDB.Population);
            PlanetEntity = colonyInfoDB.PlanetEntity;
            MineralStockpile = new JDictionary<MineralSD, int>(colonyInfoDB.MineralStockpile);
        }

        public override object Clone()
        {
            return new ColonyInfoDB(this);
        }
    }
}
