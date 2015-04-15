using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ColonyInfoDB : BaseDataBlob
    {
        public JDictionary<Entity, double> Population { get; set; }

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
        }

        public ColonyInfoDB(Entity species, double populationInMillions, Entity planet)
        {
            Population = new JDictionary<Entity, double> {{species, populationInMillions}};
            PlanetEntity = planet;
        }

        public ColonyInfoDB(ColonyInfoDB colonyInfoDB)
        {
            Population = new JDictionary<Entity, double>();
            PlanetEntity = colonyInfoDB.PlanetEntity;
            foreach (KeyValuePair<Entity, double> keyValue in colonyInfoDB.Population)
            {
                Population.Add(keyValue.Key, keyValue.Value);
            }

        }
    }
}
