using Newtonsoft.Json;
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
            foreach (Entity objectEntity in starSystem.SystemManager.GetAllEntitiesWithDataBlob<AsteroidDamageDB>())
            {
                //if damage exceeds certain amount spawn new asteroids of lesser mass up to a certain point.
                //these are commented out as they do not exist.
                AsteroidDamageDB AstDmgDB = objectEntity.GetDataBlob<AsteroidDamageDB>();
                if(AstDmgDB.Health <= 0)
                   SpawnSubAsteroids(game,objectEntity);
            }
        }

        /// <summary>
        /// WeaponProcessor must call this if an asteroid is targetted to damage the asteroid.
        /// </summary>
        /// <param name="Asteroid">The asteroid being shot.</param>
        /// <param name="Damage">for how much damage, to be determined how exactly this works.</param>
        static public void OnTakingDamage(Entity Asteroid, int Damage)
        {
            AsteroidDamageDB AstDmgDB = Asteroid.GetDataBlob<AsteroidDamageDB>();
            AstDmgDB.Health = AstDmgDB.Health - Damage;
        }

        internal static void SpawnSubAsteroids(Game game,Entity Asteroid)
        {
            MassVolumeDB ADB = Asteroid.GetDataBlob<MassVolumeDB>();

            //const double massDefault = 1.5e+12; //150 B tonnes?
            const double massThreshold  = 1.5e+9; //150 M tonnes?
            if (ADB.Mass > massThreshold)
            {
                //spawn new asteroids. call the asteroid factory?

                double newMass = ADB.Mass * 0.4; //add a random factor into this? do we care? will mass be printed to the player?

                NewtonBalisticDB nDB = Asteroid.GetDataBlob<NewtonBalisticDB>();
                PositionDB pDB = Asteroid.GetDataBlob<PositionDB>();

                /// doesn't work yet
                //Entity myTarget;
                //if (!game.Systems.TryGetValue(nDB.TargetGuid, out myTarget))
                //    throw new GuidNotFoundException(nDB.TargetGuid);

                StarSystem mySystem;
                if (!game.Systems.TryGetValue(pDB.SystemGuid, out mySystem))
                    throw new GuidNotFoundException(pDB.SystemGuid);

                Entity myTarget;
                if(!mySystem.SystemManager.FindEntityByGuid(nDB.TargetGuid, out myTarget))
                    throw new GuidNotFoundException(nDB.TargetGuid);

                //public static Entity CreateAsteroid(StarSystem starSys, Entity target, DateTime collisionDate, double asteroidMass = -1.0)
                //I need the target entity, the collisionDate, and the starSystem. I may have starsystem from guid.
                //Ok so this should create the asteroid without having to add the new asteroids to a list. as that is done in the factory.
                Entity newAsteroid1 = AsteroidFactory.CreateAsteroid(mySystem,myTarget,nDB.CollisionDate,newMass);
                Entity newAsteroid2 = AsteroidFactory.CreateAsteroid(mySystem,myTarget,nDB.CollisionDate,newMass);

                mySystem.SystemManager.RemoveEntity(Asteroid);

                //Randomize the number of created asteroids?
            }
            else
            {
                //delete the existing asteroid.
                PositionDB pDB = Asteroid.GetDataBlob<PositionDB>();

                StarSystem mySystem;
                if (!game.Systems.TryGetValue(pDB.SystemGuid, out mySystem))
                    throw new GuidNotFoundException(pDB.SystemGuid);

                mySystem.SystemManager.RemoveEntity(Asteroid);
            }
        }
    }
}
