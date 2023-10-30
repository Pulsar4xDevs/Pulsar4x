using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;

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
    public class OrbitProcessor : OrbitProcessorBase, IHotloopProcessor
    {
        public TimeSpan RunFrequency => TimeSpan.FromMinutes(5);

        public TimeSpan FirstRunOffset => TimeSpan.FromTicks(0);

        public Type GetParameterType => typeof(OrbitDB);


        public void Init(Game game)
        {
            //nothing needed to do in this one. still need this function since it's required in the interface.
        }

        public void ProcessEntity(Entity entity, int deltaSeconds)
        {
            DateTime toDate = entity.Manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            UpdateOrbit(entity, entity.GetDataBlob<OrbitDB>().Parent.GetDataBlob<PositionDB>(), toDate);
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            return UpdateSystemOrbits(manager, toDate);
        }

        internal static int UpdateSystemOrbits(EntityManager manager, DateTime toDate)
        {
            //TimeSpan orbitCycle = manager.Game.Settings.OrbitCycleTime;
            //DateTime toDate = manager.ManagerSubpulses.SystemLocalDateTime + orbitCycle;
            //starSystem.SystemSubpulses.AddSystemInterupt(toDate + orbitCycle, UpdateSystemOrbits);
            //manager.ManagerSubpulses.AddSystemInterupt(toDate + orbitCycle, PulseActionEnum.OrbitProcessor);
            // Find the first orbital entity.
            Entity firstOrbital = manager.GetFirstEntityWithDataBlob<StarInfoDB>();

            if (!firstOrbital.IsValid)
            {
                // No orbitals in this manager.
                return 0;
            }

            Entity root = firstOrbital.GetDataBlob<OrbitDB>().Root;
            var rootPositionDB = root.GetDataBlob<PositionDB>();

            // Call recursive function to update every orbit in this system.
            int count = UpdateOrbit(root, rootPositionDB, toDate);
            return count;
        }

        public static int UpdateOrbit(Entity entity, PositionDB parentPositionDB, DateTime toDate)
        {
            var entityOrbitDB = entity.GetDataBlob<OrbitDB>();
            var entityPosition = entity.GetDataBlob<PositionDB>();
            int counter = 1;
            //if(toDate.Minute > entityOrbitDB.OrbitalPeriod.TotalMinutes)

            // Get our Parent-Relative coordinates.
            try
            {
                Vector3 newPosition = entityOrbitDB.GetPosition(toDate);

                // Get our Absolute coordinates.
                entityPosition.AbsolutePosition = parentPositionDB.AbsolutePosition + newPosition;

            }
            catch (OrbitProcessorException e)
            {
                //Do NOT fail to the UI. There is NO data-corruption on this exception.
                // In this event, we did NOT update our position.
                // Event evt = new Event(game.TimePulse.GameGlobalDateTime, "Non Critical Position Exception thrown in OrbitProcessor for EntityItem " + entity.Guid + " " + e.Message);
                // evt.EventType = EventType.Opps;
                // StaticRefLib.EventLog.AddEvent(evt);
            }

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
            if (UseRelativeVelocity)
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
            if (UseRelativeVelocity)
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
                if(subOrbit != null)
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

        /// <summary>
        /// Calculates a cartisian position for an intercept for a ship and an target's orbit using warp.
        /// </summary>
        /// <returns>The intercept position and DateTime</returns>
        /// <param name="mover">The entity that is trying to intercept a target.</param>
        /// <param name="targetOrbit">Target orbit.</param>
        /// <param name="atDateTime">Datetime of transit start</param>
        public static (Vector3 position, DateTime etiDateTime) GetInterceptPosition(Entity mover, OrbitDB targetOrbit, DateTime atDateTime, Vector3 offsetPosition = new Vector3())
        {
            Vector3 moverPos = mover.GetAbsoluteFuturePosition(atDateTime);
            double spd_m = mover.GetDataBlob<WarpAbilityDB>().MaxSpeed;
            return OrbitMath.GetInterceptPosition_m(moverPos, spd_m, targetOrbit, atDateTime, offsetPosition);
        }

        internal class OrbitProcessorException : Exception
        {
            public override string Message { get; }
            public Entity Entity { get; }

            public OrbitProcessorException(string message, Entity entity)
            {
                Message = message;
                Entity = entity;
            }
        }

        #endregion
    }

    public class ChangeSOIProcessor : IInstanceProcessor
    {
        internal override void ProcessEntity(Entity entity, DateTime atDateTime)
        {
            var state = entity.GetAbsoluteState();
            var parent = entity.GetSOIParentEntity();
            var grandparent = parent.GetSOIParentEntity();
            var newParent = grandparent == null ? parent : grandparent;

            var myMass = entity.GetDataBlob<MassVolumeDB>().MassTotal;
            var gpMass = newParent.GetDataBlob<MassVolumeDB>().MassTotal;
            var neworbit = OrbitDB.FromVector(newParent, myMass, gpMass, state.pos, state.Velocity, atDateTime);
            entity.SetDataBlob(neworbit);
        }
    }



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
        }

        public int ProcessManager(EntityManager manager, int deltaSeconds)
        {
            var orbits = manager.GetAllDataBlobsOfType<OrbitUpdateOftenDB>();
            DateTime toDate = manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(deltaSeconds);
            foreach (var orbit in orbits)
            {
                UpdateOrbit(orbit, toDate);
            }

            return orbits.Count;
        }

        public static void UpdateOrbit(OrbitUpdateOftenDB entityOrbitDB, DateTime toDate)
        {

            PositionDB entityPosition = entityOrbitDB.OwningEntity.GetDataBlob<PositionDB>();
            try
            {
                Vector3 newPosition = entityOrbitDB.GetPosition(toDate);
                entityPosition.RelativePosition = newPosition;
            }
            catch (OrbitProcessor.OrbitProcessorException e)
            {
                var entity = e.Entity;
                string name = "Un-Named";
                if (entity.HasDataBlob<NameDB>())
                    name = entity.GetDataBlob<NameDB>().OwnersName;
                //Do NOT fail to the UI. There is NO data-corruption on this exception.
                // In this event, we did NOT update our position.
                // Event evt = new Event(game.TimePulse.GameGlobalDateTime, "Non Critical Position Exception thrown in OrbitProcessor for EntityItem " + name + " " + entity.Guid + " " + e.Message);
                // evt.EventType = EventType.Opps;
                // StaticRefLib.EventLog.AddEvent(evt);
            }
        }
    }
}