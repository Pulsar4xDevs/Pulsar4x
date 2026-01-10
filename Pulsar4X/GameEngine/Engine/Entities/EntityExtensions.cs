using Pulsar4X.Orbital;
using System;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using System.Collections.Generic;
using System.Reflection;
using Pulsar4X.Colonies;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.GeoSurveys;
using Pulsar4X.JumpPoints;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;

namespace Pulsar4X.Extensions
{
    public static class EntityExtensions
    {
        public static string GetDefaultName(this Entity entity)
        {
            if (entity.IsValid && entity.HasDataBlob<NameDB>())
                return entity.GetDataBlob<NameDB>().DefaultName;
            return "Unknown";
        }

        public static string GetOwnersName(this Entity entity)
        {
            if (entity.IsValid && entity.HasDataBlob<NameDB>())
                return entity.GetDataBlob<NameDB>().OwnersName;
            return "Unknown";
        }

        public static string GetName(this Entity entity, int factionID)
        {
            if (entity.IsValid && entity.HasDataBlob<NameDB>())
                return entity.GetDataBlob<NameDB>().GetName(factionID);
            return "Unknown";
        }


        /// <summary>
        /// Gets the Sphere of influence parent (the entity this object is orbiting) for a given entity.
        /// *Does not check if the entity is infact within the sphere of influence, just the current position heirarchy.*
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="positionDB">provide this to save looking it up</param>
        /// <returns></returns>
        public static Entity? GetSOIParentEntity(this Entity entity, PositionDB? positionDB = null)
        {
            if(positionDB == null)
                return entity.TryGetDataBlob<PositionDB>(out positionDB) ? positionDB.Parent : null;

            return positionDB.Parent;
        }


        public static double GetSOI_m(this Entity entity)
        {
            if(entity.TryGetDataBlob<OrbitDB>(out var orbitDB) && orbitDB.Parent != null) //if we're not the parent star
            {
                var semiMajAxis = orbitDB.SemiMajorAxis;

                var myMass = entity.GetDataBlob<MassVolumeDB>().MassDry;

                var parentMass = orbitDB.Parent.GetDataBlob<MassVolumeDB>().MassDry;

                return OrbitMath.GetSOI(semiMajAxis, myMass, parentMass);
            }
            else return double.PositiveInfinity; //if we're the parent star, then soi is infinate.
        }

        /// <summary>
        /// Gets the SOI radius of a given body.
        /// </summary>
        /// <returns>The SOI radius in AU</returns>
        /// <param name="entity">Entity which has OrbitDB and MassVolumeDB</param>
        public static double GetSOI_AU(this Entity entity)
        {
            return Distance.MToAU(entity.GetSOI_m());
        }

        public static double GetFuelPercent(this Entity entity, CargoDefinitionsLibrary cargoLibrary)
        {
            if(entity.TryGetDataBlob<ShipInfoDB>(out var shipInfoDB) && entity.TryGetDataBlob<CargoStorageDB>(out var volumeStorageDB))
            {
                string thrusterFuel = String.Empty;
                foreach(var component in shipInfoDB.Design.Components.ToArray())
                {
                    if(!component.design.TryGetAttribute<NewtonionThrustAtb>(out var newtonionThrustAtb)) continue;
                    thrusterFuel = newtonionThrustAtb.FuelType;
                    break;
                }

                if(thrusterFuel == String.Empty) return 0;

                var fuelType = cargoLibrary.GetAny(thrusterFuel);
                if(fuelType == null) return 0;

                var typeStore = volumeStorageDB.TypeStores[fuelType.CargoTypeID];
                var freeVolume = volumeStorageDB.GetFreeVolume(fuelType.CargoTypeID);
                var percentFree = (freeVolume / typeStore.MaxVolume) * 100;
                var percentStored = Math.Round( 100 - percentFree, 3);

                return percentStored;
            }
            return 0;
        }

        public static (ICargoable?, double) GetFuelInfo(this Entity entity, CargoDefinitionsLibrary cargoLibrary)
        {
            if(entity.TryGetDataBlob<ShipInfoDB>(out var shipInfoDB) && entity.TryGetDataBlob<CargoStorageDB>(out var volumeStorageDB))
            {
                string thrusterFuel = String.Empty;
                foreach(var component in shipInfoDB.Design.Components.ToArray())
                {
                    if(!component.design.TryGetAttribute<NewtonionThrustAtb>(out var newtonionThrustAtb)) continue;
                    thrusterFuel = newtonionThrustAtb.FuelType;
                    break;
                }

                if(thrusterFuel == String.Empty) return (null, 0);

                var fuelType = cargoLibrary.GetAny(thrusterFuel);
                if(fuelType == null) return (null, 0);

                var typeStore = volumeStorageDB.TypeStores[fuelType.CargoTypeID];
                var freeVolume = volumeStorageDB.GetFreeVolume(fuelType.CargoTypeID);
                var percentFree = freeVolume / typeStore.MaxVolume;
                var percentStored = Math.Round( 1 - percentFree, 3);

                return (fuelType, percentStored);
            }

            return (null, 0);
        }

        // Extension method to check if all dependencies are present for a given entity.
        internal static bool AreAllDependenciesPresent(this IHasDataBlobs entity)
        {
            List<BaseDataBlob> dataBlobs = entity.GetAllDataBlobs();
            HashSet<Type> entityDataBlobTypes = new();
            HashSet<Type> requiredDataBlobTypes = new();
            foreach (BaseDataBlob blob in dataBlobs)
            {
                entityDataBlobTypes.Add(blob.GetType());

                // List<Type> dependencies = GetDependencies(blob);
                List<Type> dependencies = new();
                { // Inlined Method
                    // TODO: Consider removing this reflection for something more type-safe. Out-Of-Scope for this refactor.
                    MethodInfo? method = blob.GetType().GetMethod("GetDependencies", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
                    if (method == null)
                        continue;

                    var blobDependencies = method.Invoke(null, null) as List<Type>;
                    dependencies.AddRange(blobDependencies ?? new List<Type>());
                }

                foreach (Type dependency in dependencies)
                {
                    requiredDataBlobTypes.Add(dependency);
                }
            }

            // Now Compare the two HashSets to make sure entityDataBlobTypes has all requiredDataBlobTypes
            return requiredDataBlobTypes.IsSubsetOf(entityDataBlobTypes);
        }

        /// <summary>
        /// Returns true if the entity or one of it's direct children is a colony.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>true and the colony ID or false and -1</returns>
        public static (bool, int) IsOrHasColony(this Entity entity)
        {
            if(entity.HasDataBlob<ColonyInfoDB>()) return (true, entity.Id);

            if(entity.TryGetDataBlob<PositionDB>(out var positionDB))
            {
                foreach(var child in positionDB.Children)
                {
                    if(child.HasDataBlob<ColonyInfoDB>())
                        return (true, child.Id);
                }
            }

            return (false, -1);
        }

        /// <summary>
        /// Checks if the entity has the ability to conduct geo-surveys
        /// </summary>
        /// <param name="entity"></param>
        /// <returns>True if itself or any child entities in a fleet have the ability to conduct geo-surveys</returns>
        public static bool HasGeoSurveyAbility(this Entity entity)
        {
            if(entity.HasDataBlob<GeoSurveyAbilityDB>()) return true;

            if(entity.TryGetDataBlob<FleetDB>(out var fleetDB))
            {
                foreach(var child in fleetDB.Children)
                {
                    if(child.HasGeoSurveyAbility())
                        return true;
                }
            }

            return false;
        }

        public static bool HasJPSurveyAbililty(this Entity entity)
        {
            if(entity.HasDataBlob<JPSurveyAbilityDB>()) return true;

            if(entity.TryGetDataBlob<FleetDB>(out var fleetDB))
            {
                foreach(var child in fleetDB.Children)
                {
                    if(child.HasJPSurveyAbililty())
                        return true;
                }
            }

            return false;
        }

        public static CargoDefinitionsLibrary? GetFactionCargoDefinitions(this Entity entity)
        {
            if(entity.GetFactionOwner.TryGetDataBlob<FactionInfoDB>(out var factionInfoDB))
            {
                return factionInfoDB.Data.CargoGoods;
            }

            return null;
        }
    }
}
