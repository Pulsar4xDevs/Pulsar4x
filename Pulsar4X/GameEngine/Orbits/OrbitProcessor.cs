using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.Events;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;

namespace Pulsar4X.Orbits
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
            ProcessEntity(entity, toDate);
        }

        public static void ProcessEntity(Entity entity, DateTime toDateTime)
        {
            var db = entity.GetDataBlob<OrbitDB>();
            UpdateOrbit(entity, db.Parent.GetDataBlob<PositionDB>(), toDateTime);
            MoveStateProcessor.ProcessForType(db, toDateTime);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            return UpdateSystemOrbits(manager, toDate);
        }

        internal static int UpdateSystemOrbits(EntityManager manager, DateTime toDate)
        {
            var orbits = manager.GetAllDataBlobsOfType<OrbitDB>();

            // Pre-calculate true anomalies once to avoid triple calculation
            var trueAnomalies = new double[orbits.Count];
            for (int i = 0; i < orbits.Count; i++)
            {
                trueAnomalies[i] = OrbitMath.GetTrueAnomaly(orbits[i], toDate);
                Vector3 newPosition = OrbitMath.GetPosition(orbits[i], trueAnomalies[i]);
                orbits[i]._position = (Vector2)newPosition;
            }

            MoveStateProcessor.ProcessForType(orbits, toDate, trueAnomalies);
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


        #region Orbit RelativePosition Calculations

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
                var subOrbit = OrbitMath.FindSOIForOrbit(orbit, AbsolutePosition);
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
            var posDB = entity.GetDataBlob<PositionDB>();
            var parent = posDB.Parent;
            if(parent == null) throw new NullReferenceException("parent cannot be null");

            // Guard: verify entity is actually near the SOI boundary.
            // If an EnterSOIProcessor fired first and changed the orbit, this interrupt is stale.
            var oldOrbit = entity.GetDataBlob<OrbitDB>();
            var shipRelPos = oldOrbit.GetPosition(atDateTime);
            var soiRadius = parent.GetSOI_m();
            if (shipRelPos.Length() < soiRadius * 0.9)
                return;

            var grandparent = parent.GetSOIParentEntity();
            var newParent = grandparent == null ? parent : grandparent;

            if (!parent.HasDataBlob<OrbitDB>())
                return; // parent is the root star, can't exit further

            // Compute position and velocity relative to newParent from orbit equations
            // at atDateTime. GetRelativeFuturePosition/GetAbsoluteFutureVelocity return
            // values in the orbit parent's frame (not the position parent's), so we must
            // do the frame conversion manually.
            var parentOrbit = parent.GetDataBlob<OrbitDB>();
            var parentRelPos = parentOrbit.GetPosition(atDateTime);
            var parentRelVel = OrbitMath.InstantaneousOrbitalVelocityVector_m(parentOrbit, atDateTime);

            // Ship position/velocity relative to orbit parent (e.g., Moon)
            var shipRelVel = OrbitMath.InstantaneousOrbitalVelocityVector_m(oldOrbit, atDateTime);

            // Convert to newParent frame (e.g., Earth): add parent's state relative to newParent
            var relPos = parentRelPos + shipRelPos;
            var relVel = parentRelVel + shipRelVel;

            var myMass = entity.GetDataBlob<MassVolumeDB>().MassTotal;
            var gpMass = newParent.GetDataBlob<MassVolumeDB>().MassTotal;

            posDB.SetParent(newParent);

            var neworbit = OrbitDB.FromVector(newParent, myMass, gpMass, relPos, relVel, atDateTime);
            entity.SetDataBlob(neworbit);

            // SetDataBlob silently overwrites the old orbit without cleanup.
            // Null OwningEntity so MoveStateProcessor won't re-process the stale orbit.
            oldOrbit.OwningEntity = null;

            // Override stale RelativePosition from SetParent with orbit-equation-derived value
            posDB.RelativePosition = relPos;

            var soievent = Event.Create(EventType.SOIChanged, atDateTime,
                "Exited SOI of " + parent.GetDefaultName(),
                entity.FactionOwnerID, entity.Manager.ManagerID);
            EventManager.Instance.Publish(soievent);
        }
    }

    public class EnterSOIProcessor : IInstanceProcessor
    {
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            if (!entity.HasDataBlob<OrbitDB>()) return;

            var posDB = entity.GetDataBlob<PositionDB>();
            var parent = posDB.Parent;
            if (parent == null || !parent.HasDataBlob<OrbitDB>()) return;

            var shipOrbit = entity.GetDataBlob<OrbitDB>();
            var parentOrbit = parent.GetDataBlob<OrbitDB>();

            // Compute ship position at atDateTime from orbit equations (not stale PositionDB)
            // Both ship and children orbit the same parent, so positions are in the same frame
            var shipRelPos = shipOrbit.GetPosition(atDateTime);

            Entity? targetChild = null;
            double closestDist = double.MaxValue;
            Vector3 targetChildRelPos = Vector3.Zero;

            foreach (var child in parentOrbit.Children)
            {
                if (child == entity) continue;
                if (!child.HasDataBlob<OrbitDB>() || !child.HasDataBlob<MassVolumeDB>()) continue;

                var childSOI = child.GetSOI_m();
                if (childSOI <= 0 || double.IsInfinity(childSOI)) continue;

                var childOrbit = child.GetDataBlob<OrbitDB>();
                var childRelPos = childOrbit.GetPosition(atDateTime);
                var dist = (shipRelPos - childRelPos).Length();

                if (dist < childSOI && dist < closestDist)
                {
                    closestDist = dist;
                    targetChild = child;
                    targetChildRelPos = childRelPos;
                }
            }

            if (targetChild == null) return;  // Stale interrupt or edge case

            // Compute relative position and velocity from orbit equations at atDateTime
            var relPos = shipRelPos - targetChildRelPos;

            var shipVel = OrbitMath.InstantaneousOrbitalVelocityVector_m(shipOrbit, atDateTime);
            var childVel = OrbitMath.InstantaneousOrbitalVelocityVector_m(targetChild.GetDataBlob<OrbitDB>(), atDateTime);
            var relVel = shipVel - childVel;

            var myMass = entity.GetDataBlob<MassVolumeDB>().MassTotal;
            var childMass = targetChild.GetDataBlob<MassVolumeDB>().MassTotal;

            // Hold a reference to the old orbit before it gets overwritten
            var oldOrbit = shipOrbit;

            posDB.SetParent(targetChild);

            var newOrbit = OrbitDB.FromVector(targetChild, myMass, childMass, relPos, relVel, atDateTime);
            entity.SetDataBlob(newOrbit);  // Triggers OnSetToEntity → may schedule SOI exit or further entry

            // EntityManager.SetDataBlob silently overwrites the old orbit in the store
            // without calling OnRemovedFromEntity or nulling OwningEntity. The old orbit
            // still points at our entity, so MoveStateProcessor would re-process it,
            // see a parent mismatch (old orbit parent=Earth vs posDB parent=Moon), call
            // SetParent(Earth), and overwrite RelativePosition with the old Earth-relative
            // value — producing the "snap to old position" bug. Null OwningEntity so
            // MoveStateProcessor skips the stale orbit.
            oldOrbit.OwningEntity = null;

            // Override stale RelativePosition (computed from stale AbsolutePosition in
            // SetParent) with the correct orbit-equation-derived value
            posDB.RelativePosition = relPos;

            var soievent = Event.Create(EventType.SOIChanged, atDateTime,
                "Entered SOI of " + targetChild.GetDefaultName(),
                entity.FactionOwnerID, entity.Manager.ManagerID);
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
            Vector3 newPosition = entityOrbitDB.GetPosition(toDate);
            entityOrbitDB._position = (Vector2)newPosition;

        }
    }
}