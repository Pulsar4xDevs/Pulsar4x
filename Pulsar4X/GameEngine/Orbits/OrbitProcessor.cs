using System;
using System.Collections.Generic;
using GameEngine.Movement;
using GameEngine.WarpMove;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.Events;

namespace Pulsar4X.Engine
{
    /// <summary>
    /// Orbit processor.
    /// How Orbits are calculated:
    /// First we get the time since epoch. (time from when the planet is at its closest to it's parent)
    /// Then we get the Mean Anomaly. (stored)
    /// Eccentric Anomaly is calculated from the Mean Anomaly, and takes the most work.
    /// True Anomaly, is calculated using the Eccentric Anomaly this is the angle from the parent (or focal point of the ellipse) to the body.
    /// With the true anomaly, we can then use trig to calculate the position.
    /// </summary>
    public class OrbitProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromMinutes(5);

        public TimeSpan FirstRunOffset => TimeSpan.FromTicks(0);

        public Type GetParameterType => typeof(OrbitDB);
        
        private static GameSettings _gameSettings;
        
        public void Init(Game game)
        {
            _gameSettings = game.Settings;
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            DateTime toDate = entity.Manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            var db = entity.GetDataBlob<OrbitDB>();
            UpdateOrbit(entity, db.Parent.GetDataBlob<PositionDB>(), toDate);
            MoveStateProcessor.ProcessForType(db, toDate);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            return UpdateSystemOrbits(manager, toDate);
        }

        internal static int UpdateSystemOrbits(EntityManager manager, DateTime toDate)
        {
            var orbits = manager.GetAllDataBlobsOfType<OrbitDB>();
            foreach (var orbit in orbits)
            {
                Vector3 newPosition = OrbitMath.GetPosition(orbit, toDate);
                PositionDB entityPosition = orbit.OwningEntity.GetDataBlob<PositionDB>();
                entityPosition.RelativePosition = newPosition;
            }

            MoveStateProcessor.ProcessForType(orbits, toDate);
            return orbits.Count;
        }

        /// <summary>
        /// this will also update any child positions.
        /// will be slightly slower than UpdateSystemOrbits as it walks the heirarchy 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="parentPositionDB"></param>
        /// <param name="toDate"></param>
        /// <returns></returns>
        public static int UpdateOrbit(Entity entity, PositionDB parentPositionDB, DateTime toDate)
        {
            var entityOrbitDB = entity.GetDataBlob<OrbitDB>();
            var entityPosition = entity.GetDataBlob<PositionDB>();
            int counter = 1;
            
            // Get our Parent-Relative coordinates.
            Vector3 newPosition = entityOrbitDB.GetPosition(toDate);
            // Get our Absolute coordinates.
            entityPosition.AbsolutePosition = parentPositionDB.AbsolutePosition + newPosition;
            
            // Update our children.
            foreach (Entity child in entityOrbitDB.Children)
            {
                // RECURSION!
                counter += UpdateOrbit(child, entityPosition, toDate);
            }
            return counter;
        }


        #region Orbit Position Calculations

        /// <summary>
        /// Gets the orbital vector, will be either Absolute or relative depending on static bool UserelativeVelocity
        /// </summary>
        /// <returns>The orbital vector.</returns>
        /// <param name="orbit">Orbit.</param>
        /// <param name="atDateTime">At date time.</param>
        public static Vector3 GetOrbitalVector(OrbitDB orbit, DateTime atDateTime)
        {
            if (_gameSettings.UseRelativeVelocity)
            {
                return orbit.InstantaneousOrbitalVelocityVector_m(atDateTime);
            }
            else
            {
                return orbit.AbsoluteOrbitalVector_m(atDateTime);
            }
        }

        public static Vector3 GetOrbitalInsertionVector(Vector3 departureVelocity, OrbitDB targetOrbit, DateTime arrivalDateTime)
        {
            if (_gameSettings.UseRelativeVelocity)
                return departureVelocity;
            else
            {
                var targetVelocity = targetOrbit.AbsoluteOrbitalVector_m(arrivalDateTime);
                return departureVelocity - targetVelocity;
            }
        }

        public static Entity FindSOIForPosition(StarSystem starSys, Vector3 AbsolutePosition)
        {
            var orbits = starSys.GetAllDataBlobsOfType<OrbitDB>();
            var withinSOIOf = new List<Entity>();
            foreach (var orbit in orbits)
            {
                var subOrbit = orbit.FindSOIForOrbit(AbsolutePosition);
                if(subOrbit != null && subOrbit.OwningEntity != null)
                    withinSOIOf.Add(subOrbit.OwningEntity);
            }

            var closestDist = double.PositiveInfinity;
            Entity closestEntity = orbits[0].Root;
            foreach (var entity in withinSOIOf)
            {
                var pos = entity.GetDataBlob<PositionDB>().AbsolutePosition;
                var distance = (AbsolutePosition - pos).Length();
                if (distance < closestDist)
                {
                    closestDist = distance;
                    closestEntity = entity;
                }

            }
            return closestEntity;
        }


        #endregion
    }

    public class ChangeSOIProcessor : IInstanceProcessor
    {
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            
            var parent = entity.GetSOIParentEntity();
            if(parent == null) throw new NullReferenceException("parent cannot be null");
            var grandparent = parent.GetSOIParentEntity();
            var newParent = grandparent == null ? parent : grandparent;

            var vel = entity.GetAbsoluteState().Velocity;
            
            entity.GetDataBlob<PositionDB>().SetParent(newParent);
            var rpos = entity.GetRalitivePosition();
            var myMass = entity.GetDataBlob<MassVolumeDB>().MassTotal;
            var gpMass = newParent.GetDataBlob<MassVolumeDB>().MassTotal;
            var neworbit = OrbitDB.FromVector(newParent, myMass, gpMass, rpos, vel, atDateTime);
            entity.SetDataBlob(neworbit);
            var soievent = Event.Create(EventType.SOIChanged, atDateTime, "SOI changed", entity.FactionOwnerID, entity.Manager.ManagerID);
            EventManager.Instance.Publish(soievent);
        }
    }



    /// <summary>
    /// designed to be used for ships in combat etc where we need a more frequent position update. 
    /// </summary>
    public class OrbitUpdateOftenProcessor : IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromSeconds(1);

        public TimeSpan FirstRunOffset => TimeSpan.FromTicks(0);

        public Type GetParameterType => typeof(OrbitUpdateOftenDB);


        public void Init(Game game)
        {
            //nothing needed to do in this one. still need this function since it's required in the interface.
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            var orbit = entity.GetDataBlob<OrbitUpdateOftenDB>();
            DateTime toDate = entity.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            UpdateOrbit(orbit, toDate);
            MoveStateProcessor.ProcessForType(orbit, toDate);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var orbits = manager.GetAllDataBlobsOfType<OrbitUpdateOftenDB>();
            DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            foreach (var orbit in orbits)
            {
                UpdateOrbit(orbit, toDate);
            }
            MoveStateProcessor.ProcessForType(orbits, toDate);
            return orbits.Count;
        }

        public static void UpdateOrbit(OrbitUpdateOftenDB entityOrbitDB, DateTime toDate)
        {
            PositionDB entityPosition = entityOrbitDB.OwningEntity.GetDataBlob<PositionDB>();

            Vector3 newPosition = entityOrbitDB.GetPosition(toDate);
            entityPosition.RelativePosition = newPosition;
            
        }
    }
}