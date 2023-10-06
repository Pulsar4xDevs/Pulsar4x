using System;
using Pulsar4X.Orbital;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Modding;

namespace Pulsar4X.Engine
{
    /// <summary>
    /// This class extends Orbital.OrbitalMath to use Entites and DataBlobs as parameters, and has other useful functions which are more pulsar specific.
    /// if you're using raw numbers, use Orbital.OrbitalMath over this one (it'll likely be more efficent due to not having to look up data)
    ///
    /// note multiple simular functions for doing the same thing, some of these are untested.
    /// Take care when using unless the function has a decent test in the tests project.
    /// Some simular functions with simular inputs left in for future performance testing (ie one of the two might be slightly more performant).
    /// </summary>
    ///
    
    public class OrbitMath : OrbitalMath
    {
        /// <summary>
        /// Currently this only calculates the change in velocity from 0 to planet radius * 1.1
        /// TODO: add gravity drag and atmosphere drag, and tech improvements for such.
        /// </summary>
        /// <param name="planetEntity"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static double FuelCostToLowOrbit(Entity planetEntity, double payload)
        {
            return FuelCostToLowOrbit(
                planetEntity.GetDataBlob<MassVolumeDB>().RadiusInM,
                planetEntity.GetDataBlob<MassVolumeDB>().MassDry, payload
            );
        }
        
        /// <summary>
        /// Currently this only calculates the change in velocity from 0 to planet radius +* 0.33333.
        /// TODO: add gravity drag and atmosphere drag, and tech improvements for such.  
        /// </summary>
        /// <param name="planetRadiusInM"></param>
        /// <param name="planetMassDryInKG"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static double FuelCostToLowOrbit(double planetRadiusInM, double planetMassDryInKG, double payload)
        {
            var lowOrbit = LowOrbitRadius(planetRadiusInM);

            var exaustVelocity = 3000;
            var sgp = GeneralMath.StandardGravitationalParameter(payload + planetMassDryInKG);
            Vector3 pos = lowOrbit * Vector3.UnitX;

            var vel = OrbitalMath.ObjectLocalVelocityPolar(sgp, pos, lowOrbit, 0, 0, 0);
            var fuelCost = OrbitalMath.TsiolkovskyFuelCost(payload, exaustVelocity, vel.speed);
            return fuelCost;
        }

        /// <summary>
        /// basicaly the radius of the planet * 1.1
        /// in future we may have this dependant on atmosphere (thickness and or gravity?)
        /// maybe we should return a lower and an upper bound? ie 1.05 to 1.333 which would allow some flexability with eccentricity,
        /// and as this is used by ships to get a good logistics transfer orbit, add some flavor with ship skill and risk aversion etc?
        /// </summary>
        /// <param name="planetEntity"></param>
        /// <returns></returns>
        public static double LowOrbitRadius(Entity planetEntity)
        {
            return LowOrbitRadius(planetEntity.GetDataBlob<MassVolumeDB>().RadiusInM);
        }
        
        public static double LowOrbitRadius(double planetRadiusInM)
        {
            return planetRadiusInM * 1.1;
        }

        struct orbit
        {
            public Vector3 position;
            public double T;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="moverAbsolutePos"></param>
        /// <param name="speed"></param>
        /// <param name="targetOrbit"></param>
        /// <param name="atDateTime"></param>
        /// <param name="offsetPosition">position relative to the target object we wish to stop warp.</param>
        /// <returns></returns>
        public static (Vector3 position, DateTime etiDateTime) GetInterceptPosition_m(Vector3 moverAbsolutePos, double speed, OrbitDB targetOrbit, DateTime atDateTime, Vector3 offsetPosition = new Vector3())
        {

            var pos = moverAbsolutePos;
            double tim = 0;

            var pl = new orbit()
            {
                position = moverAbsolutePos,
                T = targetOrbit.OrbitalPeriod.TotalSeconds,
            };

            double a = targetOrbit.SemiMajorAxis * 2;

            Vector3 p;
            int i;
            double tt, t, dt, a0, a1, T;
            // find orbital position with min error (coarse)
            a1 = -1.0;
            dt = 0.01 * pl.T;


            for (t=0; t< pl.T; t+=dt)
            {
                p = targetOrbit.GetAbsolutePosition_m(atDateTime + TimeSpan.FromSeconds(t));  //pl.position(sim_t + t);                     // try time t
                p += offsetPosition;
                tt = (p - pos).Length() / speed;  //length(p - pos) / speed;
                a0 = tt - t; if (a0 < 0.0) continue;              // ignore overshoots
                a0 /= pl.T;                                   // remove full periods from the difference
                a0 -= Math.Floor(a0);
                a0 *= pl.T;
                if ((a0 < a1) || (a1 < 0.0))
                {
                    a1 = a0;
                    tim = tt;
                }   // remember best option
            }
            // find orbital position with min error (fine)
            for (i = 0; i < 10; i++)                               // recursive increase of accuracy
                for (a1 = -1.0, t = tim - dt, T = tim + dt, dt *= 0.1; t < T; t += dt)
                {
                    p = targetOrbit.GetAbsolutePosition_m(atDateTime + TimeSpan.FromSeconds(t));  //p = pl.position(sim_t + t);                     // try time t
                    p += offsetPosition;
                    tt = (p - pos).Length() / speed;  //tt = length(p - pos) / speed;
                    a0 = tt - t; if (a0 < 0.0) continue;              // ignore overshoots
                    a0 /= pl.T;                                   // remove full periods from the difference
                    a0 -= Math.Floor(a0);
                    a0 *= pl.T;
                    if ((a0 < a1) || (a1 < 0.0))
                    {
                        a1 = a0;
                    tim = tt;
                    }   // remember best option
                }
            // direction
            p = targetOrbit.GetAbsolutePosition_m(atDateTime + TimeSpan.FromSeconds(tim));//pl.position(sim_t + tim);
            p += offsetPosition;
            //dir = normalize(p - pos);
            return (p, atDateTime + TimeSpan.FromSeconds(tim));
        }

        /// <summary>
        /// Time for a burn manuver in seconds
        /// </summary>
        /// <param name="ship"></param>
        /// <param name="dv"></param>
        /// <param name="mass"></param>
        /// <returns>time in seconds</returns>
        public static double BurnTime(Entity ship, double dv, double mass)
        {
            //var mass = ship.GetDataBlob<MassVolumeDB>().MassTotal;
            var ve = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            var burnRate = ship.GetDataBlob<NewtonThrustAbilityDB>().FuelBurnRate;
            double fuelBurned = TsiolkovskyFuelUse(mass, ve, dv);
            double tburn = fuelBurned / burnRate;
            return tburn;
        }

        /// <summary>
        /// Mass of fuel burned for a given DV change.
        /// </summary>
        /// <param name="ship"></param>
        /// <param name="dv"></param>
        /// <param name="mass"></param>
        /// <returns></returns>
        public static double FuelBurned(Entity ship, double dv, double mass)
        {
            var ve = ship.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            double fuelBurned = TsiolkovskyFuelUse(mass, ve, dv);
            return fuelBurned;
        }


        public static KeplerElements KeplerFromOrbitDB(OrbitDB orbitDB)
        {
            var entity = orbitDB.OwningEntity;
            var sgp = orbitDB.GravitationalParameter_m3S2;
            var state = entity.GetRelativeState();
            var epoch = entity.StarSysDateTime;
            return KeplerFromPositionAndVelocity(sgp, state.pos, state.Velocity, epoch);

        }

        /// <summary>
        /// the maximum deltaV availible (emtpy of cargo full of fuel).
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static double GetEmptyWetDV(Entity entity, CargoDefinitionsLibrary cargoLibrary)
        {
            var fuelTypeID = entity.GetDataBlob<NewtonThrustAbilityDB>().FuelType;
            var fuelType = cargoLibrary.GetAny(fuelTypeID);
            //var burnRate = entity.GetDataBlob<NewtonThrustAbilityDB>().FuelBurnRate;
            var exhaustVelocity = entity.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            var massDry = entity.GetDataBlob<MassVolumeDB>().MassDry;
            //var totalMass = entity.GetDataBlob<MassVolumeDB>().MassTotal;
            var parentMass = entity.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;

            //var cargoMass = entity.GetDataBlob<VolumeStorageDB>().TotalStoredMass;
            //var fuelMass = entity.GetDataBlob<VolumeStorageDB>().GetMassStored(fuelType);
            var fuelMassMax = entity.GetDataBlob<VolumeStorageDB>().GetMassMax(fuelType);
            var massTotal = massDry + fuelMassMax;
            //var sgp = OrbitMath.CalculateStandardGravityParameterInM3S2(massTotal, parentMass);

            return TsiolkovskyRocketEquation(massTotal, massDry, exhaustVelocity);

        }

        /// <summary>
        /// Max deltaV given a specific amount of (non fuel) cargoMass
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cargoMass">non volitile cargo</param>
        /// <returns></returns>
        public static double GetWetDV(Entity entity, double cargoMass, CargoDefinitionsLibrary cargoLibrary)
        {
            var fuelTypeID = entity.GetDataBlob<NewtonThrustAbilityDB>().FuelType;
            var fuelType = cargoLibrary.GetAny(fuelTypeID);
            //var burnRate = entity.GetDataBlob<NewtonThrustAbilityDB>().FuelBurnRate;
            var exhaustVelocity = entity.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            var massDry = entity.GetDataBlob<MassVolumeDB>().MassDry;
            //var totalMass = entity.GetDataBlob<MassVolumeDB>().MassTotal;
            var parentMass = entity.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;

            var fuelMassMax = entity.GetDataBlob<VolumeStorageDB>().GetMassMax(fuelType);
            var massCargoDry = massDry + cargoMass;
            var massTotal = massCargoDry + fuelMassMax;

            //var sgp = OrbitMath.CalculateStandardGravityParameterInM3S2(massTotal, parentMass);

            return TsiolkovskyRocketEquation(massTotal, massCargoDry, exhaustVelocity);

        }

        /// <summary>
        /// Max deltaV given a specific amount of (non fuel) cargoMass, and an ammount of fuel
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cargoMass">non volitile cargo</param>
        /// <returns></returns>
        public static double GetWetDV(Entity entity, double cargoMass, double fuelMass)
        {
            var exhaustVelocity = entity.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            var massDry = entity.GetDataBlob<MassVolumeDB>().MassDry;
            var parentMass = entity.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;

            var massCargoDry = massDry + cargoMass;
            var massTotal = massCargoDry + fuelMass;

            return TsiolkovskyRocketEquation(massTotal, massCargoDry, exhaustVelocity);

        }

        /// <summary>
        /// deltaV given a specific amount of (non fuel) cargoMass,and the current amount of carried fuel
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cargoMass">non volitile cargo</param>
        /// <returns></returns>
        public static double GetDV(Entity entity, double cargoMass, CargoDefinitionsLibrary cargoLibrary)
        {
            var exhaustVelocity = entity.GetDataBlob<NewtonThrustAbilityDB>().ExhaustVelocity;
            var massDry = entity.GetDataBlob<MassVolumeDB>().MassDry;
            var parentMass = entity.GetSOIParentEntity().GetDataBlob<MassVolumeDB>().MassTotal;
            var fuelTypeID = entity.GetDataBlob<NewtonThrustAbilityDB>().FuelType;
            var fuelType = cargoLibrary.GetAny(fuelTypeID);
            var fuelMass = entity.GetDataBlob<VolumeStorageDB>().GetMassStored(fuelType);

            var massCargoDry = massDry + cargoMass;
            var massTotal = massCargoDry + fuelMass;

            return TsiolkovskyRocketEquation(massTotal, massCargoDry, exhaustVelocity);

        }
    }
}
