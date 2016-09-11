using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    internal static class DamageProcessor
    {
        public static void Initialize()
        {
        }

        static public void Process(Game game, StarSystem starSystem)
        {

        }

        /// <summary>
        /// This will work for missiles, ships, asteroids, and populations at some point.
        /// </summary>
        /// <param name="DamageableEntity"></param>
        /// <param name="damageAmount"></param>
        public static void OnTakingDamage(Entity DamageableEntity, int damageAmount)
        {
            if(DamageableEntity.HasDataBlob<AsteroidDamageDB>())
            {
                AsteroidDamageDB AstDmgDB = DamageableEntity.GetDataBlob<AsteroidDamageDB>();
                AstDmgDB.Health = AstDmgDB.Health - damageAmount;

                if (AstDmgDB.Health <= 0)
                    SpawnSubAsteroids(DamageableEntity);
            }
            else if(DamageableEntity.HasDataBlob<ShipInfoDB>())
            {
                //TODO do some damage to a component.
                ReCalcProcessor.ReCalcAbilities(DamageableEntity);
            }

        }


        internal static void SpawnSubAsteroids(Entity Asteroid)
        {
            Game game = Asteroid.Manager.Game;
            MassVolumeDB ADB = Asteroid.GetDataBlob<MassVolumeDB>();

            //const double massDefault = 1.5e+12; //150 B tonnes?
            const double massThreshold = 1.5e+9; //150 M tonnes?
            if (ADB.Mass > massThreshold)
            {
                //spawn new asteroids. call the asteroid factory?

                double newMass = ADB.Mass * 0.4; //add a random factor into this? do we care? will mass be printed to the player?

                NewtonBalisticDB nDB = Asteroid.GetDataBlob<NewtonBalisticDB>();
                PositionDB pDB = Asteroid.GetDataBlob<PositionDB>();

                StarSystem mySystem;
                if (!game.Systems.TryGetValue(pDB.SystemGuid, out mySystem))
                    throw new GuidNotFoundException(pDB.SystemGuid);

                Entity myTarget;
                if (!mySystem.SystemManager.FindEntityByGuid(nDB.TargetGuid, out myTarget))
                    throw new GuidNotFoundException(nDB.TargetGuid);

                //public static Entity CreateAsteroid(StarSystem starSys, Entity target, DateTime collisionDate, double asteroidMass = -1.0)
                //I need the target entity, the collisionDate, and the starSystem. I may have starsystem from guid.
                //Ok so this should create the asteroid without having to add the new asteroids to a list. as that is done in the factory.
                Entity newAsteroid1 = AsteroidFactory.CreateAsteroid(mySystem, myTarget, nDB.CollisionDate, newMass);
                Entity newAsteroid2 = AsteroidFactory.CreateAsteroid(mySystem, myTarget, nDB.CollisionDate, newMass);

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
