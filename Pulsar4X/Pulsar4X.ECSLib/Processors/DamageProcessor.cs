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
        /// Damage type may eventually be required.
        /// </summary>
        /// <param name="DamageableEntity"></param>
        /// <param name="damageAmount"></param>
        public static void OnTakingDamage(Entity DamageableEntity, int damageAmount)
        {
            if (DamageableEntity.HasDataBlob<AsteroidDamageDB>())
            {
                AsteroidDamageDB AstDmgDB = DamageableEntity.GetDataBlob<AsteroidDamageDB>();
                AstDmgDB.Health = AstDmgDB.Health - damageAmount;

                if (AstDmgDB.Health <= 0)
                    SpawnSubAsteroids(DamageableEntity);
            }
            else if (DamageableEntity.HasDataBlob<ShipInfoDB>())
            {
                //do shield damage
                //do armor damage
                //for components: 
                Game game = DamageableEntity.Manager.Game;
                PositionDB ShipPosition = DamageableEntity.GetDataBlob<PositionDB>();

                StarSystem mySystem;
                if (!game.Systems.TryGetValue(ShipPosition.SystemGuid, out mySystem))
                    throw new GuidNotFoundException(ShipPosition.SystemGuid);

                ComponentInstancesDB ShipInst = DamageableEntity.GetDataBlob<ComponentInstancesDB>(); //These are ship components in this context

                int damageAttempt = 0;
                while (damageAmount > 0)
                {


                    int randValue = mySystem.RNG.Next((int)DamageableEntity.GetDataBlob<MassVolumeDB>().Volume);

                    foreach (KeyValuePair<Entity, double> pair in ShipInst.ComponentDictionary)
                    {
                        if (pair.Value > randValue)
                        {
                            //check if this component is destroyed
                            //if it isn't get density
                            MassVolumeDB mvDB = pair.Key.GetDataBlob<MassVolumeDB>();

                            double DensityThreshold = 1.0; //what should this be?
                            double dmgPercent = DensityThreshold * mvDB.Density;

                            int dmgDone = (int)(damageAmount * dmgPercent);

                            ComponentInfoDB ciDB = pair.Key.GetDataBlob<ComponentInfoDB>();
                            ComponentInstanceInfoDB ciiDB = pair.Key.GetDataBlob<ComponentInstanceInfoDB>();

                            if (ciiDB.HTKRemaining > 0) //component is not destroyed yet
                            {
                                if (dmgDone >= ciiDB.HTKRemaining) //component is definitely wrecked
                                {
                                    damageAmount = damageAmount - ciiDB.HTKRemaining;
                                    ciiDB.HTKRemaining = 0;
                                }
                                else
                                {
                                    ciiDB.HTKRemaining = ciiDB.HTKRemaining - damageAmount;
                                    damageAmount = 0;

                                }
                            }
                            else
                            {
                                damageAttempt++;
                                if (damageAttempt == 20) // Aurora default, seems like an ok number to use for now.
                                    break;
                                /// <summary>
                                /// Need to pick a new component to try and destroy.
                                /// Should any damage get absorbed by the wreck?
                                /// How many of these failures should I run into before declaring the ship destroyed?
                                /// Should ship distruction happen differently?
                                /// </summary>
                                continue;
                            }


                            //compare this density to some density value to calculate how much to modify damage by
                            //if damage is greater than the HTK then the component is destroyed. modify damageAmount and move onto the next component.
                            //leave this loop if damage is zero.

                            break;
                        }
                    }
                    if (damageAttempt == 20) // need to copy this to fully break out of the loop;
                        break;
                }

                if (damageAttempt == 20) // the ship is destroyed. how to mark it as such?
                {
                    SpawnWreck(DamageableEntity);
                }
                else
                {
                    ReCalcProcessor.ReCalcAbilities(DamageableEntity);
                }
            }
            else if (DamageableEntity.HasDataBlob<ColonyInfoDB>())
            {
                //Think about how to unify this one and shipInfoDB if possible.
                //do Terraforming/Infra/Pop damage
                Game game = DamageableEntity.Manager.Game;

                ColonyInfoDB ColIDB = DamageableEntity.GetDataBlob<ColonyInfoDB>();
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
                ComponentInstancesDB ColInst = DamageableEntity.GetDataBlob<ComponentInstancesDB>(); //These are installations in this context
                int damageAttempt = 0;
                while (damageAmount > 0)
                {
                    int randValue = mySystem.RNG.Next((int)DamageableEntity.GetDataBlob<MassVolumeDB>().Volume);

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
                            ComponentInstanceInfoDB ciiDB = pair.Key.GetDataBlob<ComponentInstanceInfoDB>();

                            if (ciiDB.HTKRemaining > 0) //Installation is not destroyed yet
                            {
                                if (dmgDone >= ciiDB.HTKRemaining) //Installation is definitely wrecked
                                {
                                    damageAmount = damageAmount - ciiDB.HTKRemaining;
                                    ciiDB.HTKRemaining = 0;
                                }
                                else
                                {
                                    ciiDB.HTKRemaining = ciiDB.HTKRemaining - damageAmount;
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
                ReCalcProcessor.ReCalcAbilities(DamageableEntity);
            }
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

            StarSystem mySystem;
            if (!game.Systems.TryGetValue(pDB.SystemGuid, out mySystem))
                throw new GuidNotFoundException(pDB.SystemGuid);

            //Does anything else need to be done to delete a ship?

            mySystem.SystemManager.RemoveEntity(DestroyedShip);
        }

        /// <summary>
        /// This asteroid was destroyed, see if it is big enough for child asteroids to spawn, and if so spawn them.
        /// </summary>
        /// <param name="Asteroid"></param>
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
