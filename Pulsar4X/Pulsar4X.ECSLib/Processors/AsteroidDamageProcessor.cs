using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib.Processors
{
    static public class AsteroidDamageProcessor
    {

        /// <summary>
        /// Check through the asteroids, if one has been damaged a certain amount either delete it or spawn new asteroids
        /// </summary>
        /// <param name="game"></param>
        /// <param name="starSystem"></param>
        static public void Process(Game game, StarSystem starSystem)
        {
            foreach (Entity objectEntity in starSystem.SystemManager.GetAllEntitiesWithDataBlob<NewtonBalisticDB>())
            {
                //if damage exceeds certain amount spawn new asteroids of lesser mass up to a certain point.
                //these are commented out as they do not exist.
                //AsteroidDamageDB AstDmgDB = objectEntity.GetDataBlob<AsteroidDamageDB>();
                //if(AstDmgDB.Health <= 0)
                   //SpawnSubAsteroids(objectEntity);
            }
        }

        /// <summary>
        /// WeaponProcessor must call this if an asteroid is targetted to damage the asteroid
        /// </summary>
        /// <param name="Asteroid"></param>
        /// <param name="Damage"></param>
        static public void OnTakingDamage(Entity Asteroid, int Damage)
        {
            //commented out as they do not exist yet.
            //AsteroidDamageDB AstDmgDB = objectEntity.GetDataBlob<AsteroidDamageDB>();
            //AstDmgDB.Health = AstDmgDB.Health - Damage;
        }

        internal static void SpawnSubAsteroids(Entity Asteroid)
        {
            MassVolumeDB ADB = Asteroid.GetDataBlob<MassVolumeDB>();

            int SOME_THRESHOLD = 100; //this value isn't intended to be even remotely accurate right now
            if (ADB.Mass > SOME_THRESHOLD)
            {
                //spawn new asteroids. call the asteroid factory?
            }
            else
            {
                //delete the existing asteroid. how do I remove asteroids?
            }
        }
    }
}
