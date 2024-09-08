using Pulsar4X.Orbital;
using System;
using Pulsar4X.Components;
using Pulsar4X.Atb;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using System.Collections.Generic;
using System.Reflection;

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

        public static PositionDB? GetSOIParentPositionDB(this Entity entity)
        {
            return (PositionDB?)entity.GetDataBlob<PositionDB>().ParentDB;
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
                return entity.TryGetDatablob<PositionDB>(out positionDB) ? positionDB.Parent : null;

            return positionDB.Parent;
        }

        public static (Vector3 pos, Vector3 Velocity) GetRelativeState(this Entity entity)
        {
            var pos = entity.GetDataBlob<PositionDB>().RelativePosition;
            var datetime = entity.StarSysDateTime;
            if (entity.HasDataBlob<OrbitDB>())
            {
                datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitDB>();

                var vel = orbit.InstantaneousOrbitalVelocityVector_m(datetime);
                return (pos, vel);
            }
            if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitUpdateOftenDB>();
                var vel = orbit.InstantaneousOrbitalVelocityVector_m(datetime);
                return (pos, vel);
            }

            if (entity.HasDataBlob<NewtonSimDB>())
            {
                var move = entity.GetDataBlob<NewtonSimDB>();

                var vel = move.CurrentVector_ms;
                return (pos, vel);
            }

            if (entity.HasDataBlob<NewtonSimpleMoveDB>())
            {
                NewtonSimpleProcessor.GetRelativeState(entity, datetime);
            }

            if (entity.HasDataBlob<ColonyInfoDB>())
            {
                var daylen = entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.GetDataBlob<SystemBodyInfoDB>().LengthOfDay.TotalSeconds;
                var radius = pos.Length();
                var d = 2 * Math.PI * radius;
                double speed = 0;
                if(daylen !=0)
                   speed = d / daylen;

                Vector3 vel = new Vector3(0, speed, 0);

                var posAngle = Math.Atan2(pos.Y, pos.X);
                var mtx = Matrix3d.IDRotateZ(posAngle + (Math.PI * 0.5));

                Vector3 transformedVector = mtx.Transform(vel);
                return (pos, transformedVector);
            }
            if(entity.HasDataBlob<WarpMovingDB>())
            {
                var warpdb = entity.GetDataBlob<WarpMovingDB>();
                return (pos, warpdb.CurrentNonNewtonionVectorMS);
            }
            else
            {
                return(pos, Vector3.Zero);
            }
        }

        public static (Vector3 pos, Vector3 Velocity) GetAbsoluteState(this Entity entity)
        {
            var posdb = entity.GetDataBlob<PositionDB>();
            var pos = posdb.AbsolutePosition;
            if (entity.HasDataBlob<OrbitDB>())
            {
                var datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitDB>();
                var vel = orbit.InstantaneousOrbitalVelocityVector_m(datetime);
                if (posdb.Parent != null)
                {
                    vel += posdb.Parent.GetAbsoluteState().Velocity;
                }

                return (pos, vel);
            }
            if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                var datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitUpdateOftenDB>();
                var vel = orbit.InstantaneousOrbitalVelocityVector_m(datetime);
                if (posdb.Parent != null)
                {
                    vel += posdb.Parent.GetAbsoluteState().Velocity;
                }
                return (pos, vel);
            }

            if (entity.HasDataBlob<NewtonSimDB>())
            {
                var move = entity.GetDataBlob<NewtonSimDB>();
                var vel = move.CurrentVector_ms;
                return (pos, vel);
            }
            
            if (entity.HasDataBlob<NewtonSimpleMoveDB>())
            {
                return  NewtonSimpleProcessor.GetAbsoluteState(entity, entity.StarSysDateTime);
            }
            
            if(entity.HasDataBlob<WarpMovingDB>())
            {
                var vel = entity.GetDataBlob<WarpMovingDB>().CurrentNonNewtonionVectorMS;
                return(pos,vel);
            }
            else
            {
                return(pos, Vector3.Zero);
            }
        }

        public static (Vector3 pos, Vector3 Velocity) GetRelativeFutureState(this Entity entity, DateTime atDateTime)
        {
            var fvel = GetRelativeFutureVelocity(entity, atDateTime);
            var fpos = GetRelativeFuturePosition(entity, atDateTime);

            return (fpos, fvel);
        }

        /// <summary>
        /// Gets future velocity for this entity, datablob agnostic.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="atDateTime"></param>
        /// <returns>Velocity in m/s relative to SOI parent</returns>
        /// <exception cref="Exception"></exception>
        public static Vector3 GetRelativeFutureVelocity(this Entity entity, DateTime atDateTime)
        {

            if (entity.HasDataBlob<OrbitDB>())
            {
                return entity.GetDataBlob<OrbitDB>().InstantaneousOrbitalVelocityVector_m(atDateTime);
            }
            if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                return entity.GetDataBlob<OrbitUpdateOftenDB>().InstantaneousOrbitalVelocityVector_m(atDateTime);
            }
            else if (entity.HasDataBlob<NewtonSimDB>())
            {
                return NewtonSimProcessor.GetRelativeState(entity, entity.GetDataBlob<NewtonSimDB>(), atDateTime).vel;
            }
            else if (entity.HasDataBlob<NewtonSimpleMoveDB>())
            {
                return NewtonSimpleProcessor.GetRelativeState(entity, atDateTime).vel;
            }
            else if (entity.HasDataBlob<WarpMovingDB>())
            {
                return entity.GetDataBlob<WarpMovingDB>().SavedNewtonionVector;
            }
            else
            {
                throw new Exception("Entity has no velocity");
            }
        }

        /// <summary>
        /// Gets future velocity for this entity, datablob agnostic.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="atDateTime"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Vector3 GetAbsoluteFutureVelocity(this Entity entity, DateTime atDateTime)
        {
            if (entity.HasDataBlob<OrbitDB>())
            {
                return entity.GetDataBlob<OrbitDB>().AbsoluteOrbitalVector_m(atDateTime);
            }
            if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                return entity.GetDataBlob<OrbitUpdateOftenDB>().AbsoluteOrbitalVector_m(atDateTime);
            }
            else if (entity.HasDataBlob<NewtonSimDB>())
            {
                var vel = NewtonSimProcessor.GetRelativeState(entity, entity.GetDataBlob<NewtonSimDB>(), atDateTime).vel;
                var parentEntity = GetSOIParentEntity(entity);
                if(parentEntity == null) throw new NullReferenceException("parentEntity cannot be null");
                //recurse
                return GetAbsoluteFutureVelocity(parentEntity, atDateTime) + vel;
            }
            else if (entity.HasDataBlob<NewtonSimpleMoveDB>())
            {
                return NewtonSimpleProcessor.GetAbsoluteState(entity, atDateTime).vel;
            }
            else if (entity.HasDataBlob<WarpMovingDB>())
            {
                return entity.GetDataBlob<WarpMovingDB>().SavedNewtonionVector;
            }
            else
            {
                throw new Exception("Entity has no velocity");
            }
        }



        /// <summary>
        /// Gets a future position for this entity, regarless of wheter it's orbit or newtonion trajectory
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="atDateTime"></param>
        /// <returns>In Meters</returns>
        /// <exception cref="Exception"> if entity doesn't have one of the correct datablobs</exception>
        public static Vector3 GetRelativeFuturePosition(this Entity entity, DateTime atDateTime)
        {
            if (entity.HasDataBlob<OrbitDB>())
            {
                return entity.GetDataBlob<OrbitDB>().GetPosition(atDateTime);
            }
            else if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                return entity.GetDataBlob<OrbitUpdateOftenDB>().GetPosition(atDateTime);
            }
            else if (entity.HasDataBlob<NewtonSimDB>())
            {
                return NewtonSimProcessor.GetRelativeState(entity, entity.GetDataBlob<NewtonSimDB>(), atDateTime).pos;
            }
            else if (entity.HasDataBlob<NewtonSimpleMoveDB>())
            {
                return NewtonSimpleProcessor.GetRelativeState(entity, atDateTime).pos;
            }
            else if (entity.HasDataBlob<PositionDB>())
            {
                return entity.GetDataBlob<PositionDB>().RelativePosition;
            }
            else
            {
                throw new Exception("Entity is positionless");
            }
        }

        /// <summary>
        /// Gets a future position for this entity, regarless of wheter it's orbit or newtonion trajectory
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="atDateTime"></param>
        /// <returns>In Meters</returns>
        /// <exception cref="Exception"> if entity doesn't have one of the correct datablobs</exception>
        public static Vector3 GetAbsoluteFuturePosition(this Entity entity, DateTime atDateTime)
        {
            if (entity.HasDataBlob<OrbitDB>())
            {
                return entity.GetDataBlob<OrbitDB>().GetAbsolutePosition_m(atDateTime);
            }
            else if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                return entity.GetDataBlob<OrbitUpdateOftenDB>().GetAbsolutePosition_m(atDateTime);
            }
            else if (entity.HasDataBlob<NewtonSimDB>())
            {
                return NewtonSimProcessor.GetAbsoluteState(entity, entity.GetDataBlob<NewtonSimDB>(), atDateTime).pos;
            }
            else if (entity.HasDataBlob<NewtonSimpleMoveDB>())
            {
                return NewtonSimpleProcessor.GetAbsoluteState(entity, atDateTime).pos;
            }
            else if (entity.HasDataBlob<PositionDB>())
            {
                return entity.GetDataBlob<PositionDB>().AbsolutePosition;
            }
            else
            {
                throw new Exception("Entity is positionless");
            }
        }

        /// <summary>
        /// For more efficent, get and store a reference to PositionDB.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Vector3 GetAbsolutePosition(this Entity entity)
        {
            return entity.GetDataBlob<PositionDB>().AbsolutePosition;
        }

        /// <summary>
        /// For more efficent, get and store a reference to PositionDB.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static Vector3 GetRalitivePosition(this Entity entity)
        {
            return entity.GetDataBlob<PositionDB>().RelativePosition;
        }

        public static double GetSOI_m(this Entity entity)
        {
            var orbitDB = entity.GetDataBlob<OrbitDB>();
            if (orbitDB.Parent != null) //if we're not the parent star
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
            if(entity.TryGetDatablob<ShipInfoDB>(out var shipInfoDB) && entity.TryGetDatablob<VolumeStorageDB>(out var volumeStorageDB))
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
            if(entity.TryGetDatablob<ShipInfoDB>(out var shipInfoDB) && entity.TryGetDatablob<VolumeStorageDB>(out var volumeStorageDB))
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

            if(entity.TryGetDatablob<PositionDB>(out var positionDB))
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

            if(entity.TryGetDatablob<FleetDB>(out var fleetDB))
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

            if(entity.TryGetDatablob<FleetDB>(out var fleetDB))
            {
                foreach(var child in fleetDB.Children)
                {
                    if(child.HasJPSurveyAbililty())
                        return true;
                }
            }

            return false;
        }
    }
}
