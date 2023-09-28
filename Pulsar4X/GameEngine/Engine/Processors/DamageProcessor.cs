using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Damage;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine
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
        /// Damage type may eventually be required.
        /// </summary>
        /// <param name="damageableEntity"></param>
        /// <param name="damageAmount"></param>
        public static List<RawBmp> OnTakingDamage(Entity damageableEntity, DamageFragment damage)
        {

            var db = damageableEntity.GetDataBlob<EntityDamageProfileDB>();
            if (!damageableEntity.HasDataBlob<EntityDamageProfileDB>())
            {
                //I think currently most damageable entites should already have this, 
                //need to consider whether an undamaged entity needs this or if we should create it if and when it gets damaged.
                
                if(damageableEntity.HasDataBlob<ShipInfoDB>())
                {
                    db = new EntityDamageProfileDB(damageableEntity.GetDataBlob<ShipInfoDB>().Design);
                    damageableEntity.SetDataBlob(db);
                }
                //return;
            }
            
             return DamageTools.DealDamage(db, damage);

            /*
            if (damageableEntity.HasDataBlob<AsteroidDamageDB>())
            {
                AsteroidDamageDB AstDmgDB = damageableEntity.GetDataBlob<AsteroidDamageDB>();
                AstDmgDB.Health = AstDmgDB.Health - damageAmount;

                if (AstDmgDB.Health <= 0)
                    SpawnSubAsteroids(damageableEntity, atDateTime);
            }
            else if (damageableEntity.HasDataBlob<ShipInfoDB>())
            {
                //do shield damage
                //do armor damage
                //for components: 
                Game game = damageableEntity.Manager.Game;
                PositionDB ShipPosition = damageableEntity.GetDataBlob<PositionDB>();

                StarSystem mySystem;
                if (!game.Systems.TryGetValue(ShipPosition.SystemGuid, out mySystem))
                    throw new GuidNotFoundException(ShipPosition.SystemGuid);

                ComponentInstancesDB instancesDB = damageableEntity.GetDataBlob<ComponentInstancesDB>(); //These are ship components in this context

                int damageAttempt = 0;
                while (damageAmount > 0)
                {

                    int randValue = mySystem.RNGNext((int)(damageableEntity.GetDataBlob<MassVolumeDB>().Volume_m3)); //volume in m^3

          
                    if (damageAttempt == 20) // need to copy this to fully break out of the loop;
                        break;
                }

                if (damageAttempt == 20) // the ship is destroyed. how to mark it as such?
                {
                    SpawnWreck(damageableEntity);
                }
                else
                {
                    ReCalcProcessor.ReCalcAbilities(damageableEntity);
                }
            }
            else if (damageableEntity.HasDataBlob<ColonyInfoDB>())
            {
                //Think about how to unify this one and shipInfoDB if possible.
                //do Terraforming/Infra/Pop damage
                Game game = damageableEntity.Manager.Game;

                ColonyInfoDB ColIDB = damageableEntity.GetDataBlob<ColonyInfoDB>();
                SystemBodyInfoDB SysInfoDB = ColIDB.PlanetEntity.GetDataBlob<SystemBodyInfoDB>();

                PositionDB ColonyPosition = ColIDB.PlanetEntity.GetDataBlob<PositionDB>();

                StarSystem mySystem; //I need all of this to get to the rng.
                if (!game.Systems.TryGetValue(ColonyPosition.SystemGuid, out mySystem))
                    throw new GuidNotFoundException(ColonyPosition.SystemGuid);

                //How should damage work here?
                //quarter million dead per strength of nuclear attack? 1 radiation/1 dust per strength?
                //Same chance to destroy components as ship destruction?

                //I need damage type for these. Missiles/bombs(missile damage but no engine basically) will be the only thing that causes this damage.
                //ColIDB.Population
                //SysInfoDB.AtmosphericDust
                //SysInfoDB.RadiationLevel


                //Installation Damage section:
                ComponentInstancesDB ColInst = damageableEntity.GetDataBlob<ComponentInstancesDB>(); //These are installations in this context
                int damageAttempt = 0;
                while (damageAmount > 0)
                {
                    int randValue = mySystem.RNGNext((int)damageableEntity.GetDataBlob<MassVolumeDB>().Volume_km3);

                    foreach (KeyValuePair<Entity, double> pair in ColInst.ComponentDictionary)
                    {
                        if (pair.Value > randValue) //This installation was targeted
                        {

                            //check if this Installation is destroyed
                            //if it isn't get density
                            MassVolumeDB mvDB = pair.Key.GetDataBlob<MassVolumeDB>();

                            double DensityThreshold = 1.0; //what should this be?
                            double dmgPercent = DensityThreshold * mvDB.Density;

                            int dmgDone = (int)(damageAmount * dmgPercent);

                            ComponentInfoDB ciDB = pair.Key.GetDataBlob<ComponentInfoDB>();
                            ComponentInstanceData cii = pair.Key.GetDataBlob<ComponentInstanceData>();

                            if (cii.HTKRemaining > 0) //Installation is not destroyed yet
                            {
                                if (dmgDone >= cii.HTKRemaining) //Installation is definitely wrecked
                                {
                                    damageAmount = damageAmount - cii.HTKRemaining;
                                    cii.HTKRemaining = 0;
                                }
                                else
                                {
                                    cii.HTKRemaining = cii.HTKRemaining - damageAmount;
                                    damageAmount = 0;

                                }
                            }
                            else
                            {
                                damageAttempt++;
                                if (damageAttempt == 20) // The planet won't blow up because of this, but no more attempts to damage installations should be made here.
                                    break;

                                continue;

                            }
                        }
                    }
                    if (damageAttempt == 20) // need to copy this to fully break out of the loop;
                        break;
                }

                //This will need to be updated to deal with colonies.
                ReCalcProcessor.ReCalcAbilities(damageableEntity);
            }
            */
        }

        /// <summary>
        /// I want to delete the existing ship, and replace it with a wreck here that can be salvaged for materials and parts.
        /// </summary>
        /// <param name="DestroyedShip"></param>
        internal static void SpawnWreck(Entity DestroyedShip)
        {
            //create the wreck here



            //Destroy the ship.
            Game game = DestroyedShip.Manager.Game;
            PositionDB pDB = DestroyedShip.GetDataBlob<PositionDB>();

            if(!game.Systems.ContainsKey(pDB.SystemGuid))
                throw new Exception(pDB.SystemGuid);

            StarSystem mySystem = game.Systems[pDB.SystemGuid];

            //Does anything else need to be done to delete a ship?

            mySystem.RemoveEntity(DestroyedShip);
        }

        /// <summary>
        /// This asteroid was destroyed, see if it is big enough for child asteroids to spawn, and if so spawn them.
        /// </summary>
        /// <param name="Asteroid"></param>
        internal static void SpawnSubAsteroids(Entity Asteroid, DateTime atDateTime)
        {
            Game game = Asteroid.Manager.Game;
            MassVolumeDB ADB = Asteroid.GetDataBlob<MassVolumeDB>();

            //const double massDefault = 1.5e+12; //150 B tonnes?
            const double massThreshold = 1.5e+9; //150 M tonnes?
            if (ADB.MassDry > massThreshold)
            {
                //spawn new asteroids. call the asteroid factory?

                double newMass = ADB.MassDry * 0.4; //add a random factor into this? do we care? will mass be printed to the player?

                OrbitDB origOrbit = Asteroid.GetDataBlob<OrbitDB>();
                PositionDB pDB = Asteroid.GetDataBlob<PositionDB>();

                EntityManager mySystem = Asteroid.Manager;
                

                var origVel = origOrbit.AbsoluteOrbitalVector_m(atDateTime);

                //public static Entity CreateAsteroid(StarSystem starSys, Entity target, DateTime collisionDate, double asteroidMass = -1.0)
                //I need the target entity, the collisionDate, and the starSystem. I may have starsystem from guid.
                //Ok so this should create the asteroid without having to add the new asteroids to a list. as that is done in the factory.
                Entity newAsteroid1 = AsteroidFactory.CreateAsteroid4(pDB.AbsolutePosition, origOrbit, atDateTime, newMass); 
                //var newOrbit = OrbitDB.FromVector(origOrbit.Parent, )
                Entity newAsteroid2 = AsteroidFactory.CreateAsteroid4(pDB.AbsolutePosition, origOrbit, atDateTime, newMass);

                mySystem.RemoveEntity(Asteroid);

                //Randomize the number of created asteroids?
            }
            else
            {
                //delete the existing asteroid.
                PositionDB pDB = Asteroid.GetDataBlob<PositionDB>();

                if(!game.Systems.ContainsKey(pDB.SystemGuid))
                    throw new Exception(pDB.SystemGuid);

                StarSystem mySystem = game.Systems[pDB.SystemGuid];

                mySystem.RemoveEntity(Asteroid);
            }
        }
    }
}
