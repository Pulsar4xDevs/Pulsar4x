using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Galaxy;

namespace Pulsar4X.Industry
{
    /// <summary>
    /// Recomputes a colony's <see cref="InfrastructureDB"/> from its installations and
    /// exposes the resulting output multiplier to the production processors.
    /// </summary>
    internal static class InfrastructureProcessor
    {
        // Conversion factors turning a building's physical footprint into the amount of
        // infrastructure support it demands: 1 unit per tonne of mass plus 1 unit per crew.
        private const double CapacityPerKg = 1.0 / 1000.0; // i.e. per tonne
        private const double CapacityPerCrew = 1.0;

        /// <summary>
        /// Sums provided vs required infrastructure capacity for a colony and writes it to
        /// the colony's <see cref="InfrastructureDB"/> (creating it if needed). Driven off the
        /// colony's ComponentInstancesDB recalc, so it runs whenever an installation changes.
        /// </summary>
        internal static void RecalcCapacity(Entity colonyEntity)
        {
            // Infrastructure only applies to colonies; ships also carry a ComponentInstancesDB.
            if (!colonyEntity.TryGetDataBlob<ColonyInfoDB>(out var colonyInfo)
                || !colonyEntity.TryGetDataBlob<ComponentInstancesDB>(out var instancesDB))
                return;

            if (!colonyEntity.TryGetDataBlob<InfrastructureDB>(out var infrastructureDB))
            {
                infrastructureDB = new InfrastructureDB();
                colonyEntity.SetDataBlob(infrastructureDB);
            }

            infrastructureDB.CapacityProvided = SumProvidedCapacity(instancesDB, colonyInfo.PlanetEntity);
            infrastructureDB.CapacityRequired = SumRequiredCapacity(instancesDB);
        }

        /// <summary>
        /// The colony's current infrastructure output multiplier (1.0 when not over capacity,
        /// or no infrastructure tracking exists).
        /// </summary>
        internal static double GetEfficiency(Entity colonyEntity)
        {
            if (colonyEntity.TryGetDataBlob<InfrastructureDB>(out var infrastructureDB))
                return infrastructureDB.Efficiency;
            return 1.0;
        }

        private static long SumProvidedCapacity(ComponentInstancesDB instancesDB, Entity bodyEntity)
        {
            double bodyGravityMps2 = 0;
            if (bodyEntity.TryGetDataBlob<SystemBodyInfoDB>(out var bodyInfo))
                bodyGravityMps2 = bodyInfo.Gravity;

            double bodyPressureAtm = 0;
            if (bodyEntity.TryGetDataBlob<AtmosphereDB>(out var atmosphere))
                bodyPressureAtm = atmosphere.Pressure;

            long provided = 0;
            foreach (var design in instancesDB.GetDesignsByType(typeof(InfrastructureCapacityAtb)))
            {
                // Out-of-tolerance infrastructure provides nothing, matching population support.
                if (design.TryGetAttribute<GravityToleranceAtb>(out var gravTol)
                    && !gravTol.SupportsBodyGravity(bodyGravityMps2))
                    continue;

                if (design.TryGetAttribute<PressureToleranceAtb>(out var pressTol)
                    && !pressTol.SupportsBodyPressure(bodyPressureAtm))
                    continue;

                long capacity = design.GetAttribute<InfrastructureCapacityAtb>().Capacity;
                foreach (var component in instancesDB.GetComponentsBySpecificDesign(design.UniqueID))
                {
                    if (component.IsEnabled)
                        provided += (long)(capacity * component.HealthPercent);
                }
            }

            return provided;
        }

        private static long SumRequiredCapacity(ComponentInstancesDB instancesDB)
        {
            long required = 0;
            foreach (var (design, count) in instancesDB.DesignsAndComponentCount)
            {
                // Infrastructure provides capacity; it doesn't consume it.
                if (design.TryGetAttribute<InfrastructureCapacityAtb>(out _))
                    continue;

                double per = design.MassPerUnit * CapacityPerKg + design.CrewReq * CapacityPerCrew;
                required += (long)(per * count);
            }

            return required;
        }
    }
}
