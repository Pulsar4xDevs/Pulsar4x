using System;
using System.Collections.Generic;
using Pulsar4X.Damage;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Orbital;
using Pulsar4X.Orbits;
using Pulsar4X.Sensors;

namespace Pulsar4X.Galaxy
{
    /// <summary>
    /// Asteroid factory. creates rocks to collide with planets
    /// </summary>
    public static class AsteroidFactory
    {
        /// <summary>
        /// creates an asteroid that will collide with the given entity on the given date.
        /// </summary>
        /// <param name="starSys"></param>
        /// <param name="target"></param>
        /// <param name="collisionDate"></param>
        /// <returns></returns>
        public static Entity CreateAsteroid(StarSystem starSys, Entity target, DateTime collisionDate, double asteroidMass = -1.0)
        {
            //todo rand these a bit.
            double radius = 500;

            double mass;
            if (asteroidMass < 0)
                mass = 1.5e+12; //about 1.5 billion tonne
            else
                mass = asteroidMass;

            var speed = 40000;
            Vector3 velocity = Vector3.UnitX*speed;


            var massVolume = MassVolumeDB.NewFromMassAndRadius_m(mass, radius);
            var planetInfo = new SystemBodyInfoDB();
            var name = new NameDB("Ellie");
            var AsteroidDmg = new AsteroidDamageDB{ FractureChance = new PercentValue(0.75f) };
            var dmgPfl = EntityDamageProfileDB.AsteroidDamageProfile(massVolume.Volume_km3, massVolume.DensityDry_gcm, massVolume.RadiusInM, 50, starSys.RNG);
            var sensorPfil = new SensorProfileDB();

            planetInfo.SupportsPopulations = false;
            planetInfo.BodyType = BodyType.Asteroid;

            Vector3 targetPos = OrbitMath.GetAbsolutePosition(target.GetDataBlob<OrbitDB>(), collisionDate);
            TimeSpan timeToCollision = collisionDate - starSys.Game.TimePulse.GameGlobalDateTime;


            var parent = target.GetDataBlob<OrbitDB>().Parent;
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var myMass = massVolume.MassDry;

            OrbitDB orbit = OrbitDB.FromVector(parent, myMass, parentMass, targetPos, velocity, collisionDate);

            var currentpos = OrbitMath.GetAbsolutePosition(orbit, starSys.Game.TimePulse.GameGlobalDateTime);
            var posDB = new PositionDB(currentpos, parent);


            var planetDBs = new List<BaseDataBlob>
            {
                name,
                posDB,
                massVolume,
                planetInfo,
                orbit,
                AsteroidDmg,
                dmgPfl,
                sensorPfil
            };

            Entity newELE = Entity.Create();
            starSys.AddEntity(newELE, planetDBs);
            return newELE;
        }

        public static Entity CreateAsteroid4(Vector3 position, OrbitDB origOrbit, DateTime atDateTime, Random rng, double asteroidMass = -1.0 )
        {
            //todo rand these a bit.
            double radius = 500;

            double mass;
            if (asteroidMass == -1.0)
                mass = 1.5e+12; //about 1.5 billion tonne
            else
                mass = asteroidMass;

            var speed = 40000;
            Vector3 velocity = Vector3.UnitX*speed;


            var massVolume = MassVolumeDB.NewFromMassAndRadius_m(mass, radius);
            var planetInfo = new SystemBodyInfoDB();
            var name = new NameDB("Ellie");
            var AsteroidDmg = new AsteroidDamageDB{ FractureChance = new PercentValue(0.75f) };
            var dmgPfl = EntityDamageProfileDB.AsteroidDamageProfile(massVolume.Volume_km3, massVolume.DensityDry_gcm, massVolume.RadiusInM, 50, rng);
            var sensorPfil = new SensorProfileDB();

            planetInfo.SupportsPopulations = false;
            planetInfo.BodyType = BodyType.Asteroid;


            var parent = origOrbit.Parent;
            var parentMass = parent.GetDataBlob<MassVolumeDB>().MassDry;
            var myMass = massVolume.MassDry;

            //OrbitDB orbit = OrbitDB.FromVector(parent, myMass, parentMass, sgp, position, velocity, atDateTime);
            //OrbitDB orbit = (OrbitDB)origOrbit.Clone();
            OrbitDB orbit = new OrbitDB(origOrbit.Parent, parentMass, myMass, Distance.MToAU(origOrbit.SemiMajorAxis),
                origOrbit.Eccentricity, origOrbit.Inclination, origOrbit.LongitudeOfAscendingNode,
                origOrbit.ArgumentOfPeriapsis, origOrbit.MeanAnomalyAtEpoch, origOrbit.Epoch);

            var posDB = new PositionDB(position, parent);

            var planetDBs = new List<BaseDataBlob>
            {
                name,
                posDB,
                massVolume,
                planetInfo,
                orbit,
                AsteroidDmg,
                dmgPfl,
                sensorPfil
            };

            Entity newELE = Entity.Create();
            origOrbit.OwningEntity.Manager.AddEntity(newELE, planetDBs);
            return newELE;
        }
    }
}
