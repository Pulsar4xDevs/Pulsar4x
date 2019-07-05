using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
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
            double radius = Distance.KmToAU(0.5);

            double mass;
            if (asteroidMass == -1.0)
                mass = 1.5e+12; //about 1.5 billion tonne
            else
                mass = asteroidMass;
            Vector3 velocity = new Vector3(8, 7, 0);

            var position = new PositionDB(0, 0, 0, Guid.Empty);
            var massVolume = MassVolumeDB.NewFromMassAndRadius(mass, radius);
            var planetInfo = new SystemBodyInfoDB();
            var balisticTraj = new NewtonBalisticDB(target.Guid,collisionDate);
            var name = new NameDB("Ellie");
            var AsteroidDmg = new AsteroidDamageDB();
            var sensorPfil = new SensorProfileDB();

            planetInfo.SupportsPopulations = false;
            planetInfo.BodyType = BodyType.Asteroid;

            Vector3 targetPos = OrbitProcessor.GetAbsolutePosition_AU(target.GetDataBlob<OrbitDB>(), collisionDate);
            TimeSpan timeToCollision = collisionDate - StaticRefLib.CurrentDateTime;


            Vector3 offset = velocity * timeToCollision.TotalSeconds;
            targetPos -=  Distance.KmToAU(offset);
            position.AbsolutePosition_AU = targetPos;
            position.SystemGuid = starSys.Guid;
            balisticTraj.CurrentSpeed = velocity;



            var planetDBs = new List<BaseDataBlob>
            {
                position,
                massVolume,
                planetInfo,
                name,
                balisticTraj,
                AsteroidDmg,
                sensorPfil
            };

            Entity newELE = new Entity(starSys, planetDBs);
            return newELE;
        }

        public static Entity CreateAsteroid2(StarSystem starSys, Entity target, DateTime collisionDate, double asteroidMass = -1.0)
        {
            //todo rand these a bit.
            double radius = Distance.KmToAU(0.5);

            double mass;
            if (asteroidMass == -1.0)
                mass = 1.5e+12; //about 1.5 billion tonne
            else
                mass = asteroidMass;
            Vector3 velocity = new Vector3(8, 7, 0);

            var position = new PositionDB(0, 0, 0, Guid.Empty);
            var massVolume = MassVolumeDB.NewFromMassAndRadius(mass, radius);
            var planetInfo = new SystemBodyInfoDB();
            var balisticTraj = new NewtonBalisticDB(target.Guid, collisionDate);
            var name = new NameDB("Ellie");
            var AsteroidDmg = new AsteroidDamageDB();
            var sensorPfil = new SensorProfileDB();

            planetInfo.SupportsPopulations = false;
            planetInfo.BodyType = BodyType.Asteroid;

            Vector3 targetPos = OrbitProcessor.GetAbsolutePosition_AU(target.GetDataBlob<OrbitDB>(), collisionDate);
            TimeSpan timeToCollision = collisionDate - StaticRefLib.CurrentDateTime;


            Vector3 offset = velocity * timeToCollision.TotalSeconds;
            targetPos -= Distance.KmToAU(offset);
            position.AbsolutePosition_AU = targetPos;
            position.SystemGuid = starSys.Guid;
            balisticTraj.CurrentSpeed = velocity;


            var parent = target.GetDataBlob<OrbitDB>().Parent;
            var parentMass = parent.GetDataBlob<MassVolumeDB>().Mass;
            var myMass = massVolume.Mass;
            var mySemiMajorAxis = 5.055;
            var myEccentricity = 0.8;
            var myInclination = 0;
            var myLoAN = 0;
            var myAoP = -10;
            //var EccentricAnomaly = Math.Atan2()
            //var meanAnomaly =;
            double myLoP = 0;
            double myMeanLongd = 355.5;
            //OrbitDB orbit = OrbitDB.FromAsteroidFormat(parent, parentMass, myMass, mySemiMajorAxis, myEccentricity, myInclination, myLoAN, myAoP, meanAnomaly, starSys.Game.CurrentDateTime); 
            OrbitDB orbit = OrbitDB.FromMajorPlanetFormat(parent, parentMass, myMass, mySemiMajorAxis, myEccentricity, myInclination, myLoAN, myLoP, myMeanLongd, StaticRefLib.CurrentDateTime);

            var planetDBs = new List<BaseDataBlob>
            {
                position,
                massVolume,
                planetInfo,
                name,
                orbit,
                AsteroidDmg,
                sensorPfil
            };

            Entity newELE = new Entity(starSys, planetDBs);
            return newELE;
        }

        public static Entity CreateAsteroid3(StarSystem starSys, Entity target, DateTime collisionDate, double asteroidMass = -1.0)
        {
            //todo rand these a bit.
            double radius = Distance.KmToAU(0.5);

            double mass;
            if (asteroidMass == -1.0)
                mass = 1.5e+12; //about 1.5 billion tonne
            else
                mass = asteroidMass;

            var speed = Distance.KmToAU(40);
            Vector3 velocity = new Vector3(speed, 0, 0);


            var massVolume = MassVolumeDB.NewFromMassAndRadius(mass, radius);
            var planetInfo = new SystemBodyInfoDB();
            var name = new NameDB("Ellie");
            var AsteroidDmg = new AsteroidDamageDB();
            AsteroidDmg.FractureChance = new PercentValue(0.75f);
            var sensorPfil = new SensorProfileDB();

            planetInfo.SupportsPopulations = false;
            planetInfo.BodyType = BodyType.Asteroid;

            Vector3 targetPos = OrbitProcessor.GetAbsolutePosition_AU(target.GetDataBlob<OrbitDB>(), collisionDate);
            TimeSpan timeToCollision = collisionDate - StaticRefLib.CurrentDateTime;


            var parent = target.GetDataBlob<OrbitDB>().Parent;
            var parentMass = parent.GetDataBlob<MassVolumeDB>().Mass;
            var myMass = massVolume.Mass;

            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + myMass) / 3.347928976e33;
            OrbitDB orbit = OrbitDB.FromVector(parent, myMass, parentMass, sgp, targetPos, velocity, collisionDate);

            var currentpos = OrbitProcessor.GetAbsolutePosition_AU(orbit, StaticRefLib.CurrentDateTime);
            var posDB = new PositionDB(currentpos.X, currentpos.Y, currentpos.Z, parent.Manager.ManagerGuid, parent);


            var planetDBs = new List<BaseDataBlob>
            {
                posDB,
                massVolume,
                planetInfo,
                name,
                orbit,
                AsteroidDmg,
                sensorPfil
            };

            Entity newELE = new Entity(starSys, planetDBs);
            return newELE;
        }

        public static Entity CreateAsteroid4(Vector3 position, OrbitDB origOrbit, DateTime atDateTime, double asteroidMass = -1.0)
        {
            //todo rand these a bit.
            double radius = Distance.KmToAU(0.5);

            double mass;
            if (asteroidMass == -1.0)
                mass = 1.5e+12; //about 1.5 billion tonne
            else
                mass = asteroidMass;

            var speed = Distance.KmToAU(40);
            Vector3 velocity = new Vector3(speed, 0, 0);


            var massVolume = MassVolumeDB.NewFromMassAndRadius(mass, radius);
            var planetInfo = new SystemBodyInfoDB();
            var name = new NameDB("Ellie");
            var AsteroidDmg = new AsteroidDamageDB();
            AsteroidDmg.FractureChance = new PercentValue(0.75f);
            var sensorPfil = new SensorProfileDB();

            planetInfo.SupportsPopulations = false;
            planetInfo.BodyType = BodyType.Asteroid;


            var parent = origOrbit.Parent;
            var parentMass = parent.GetDataBlob<MassVolumeDB>().Mass;
            var myMass = massVolume.Mass;

            double sgp = GameConstants.Science.GravitationalConstant * (parentMass + myMass) / 3.347928976e33;
            //OrbitDB orbit = OrbitDB.FromVector(parent, myMass, parentMass, sgp, position, velocity, atDateTime);
            //OrbitDB orbit = (OrbitDB)origOrbit.Clone();
            OrbitDB orbit = new OrbitDB(origOrbit.Parent, parentMass, myMass, origOrbit.SemiMajorAxisAU, 
                origOrbit.Eccentricity, origOrbit.Inclination_Degrees, origOrbit.LongitudeOfAscendingNode_Degrees, 
                origOrbit.ArgumentOfPeriapsis_Degrees, origOrbit.MeanMotion_DegreesSec, origOrbit.Epoch);

            var posDB = new PositionDB(position.X, position.Y, position.Z, parent.Manager.ManagerGuid, parent);


            var planetDBs = new List<BaseDataBlob>
            {
                posDB,
                massVolume,
                planetInfo,
                name,
                orbit,
                AsteroidDmg,
                sensorPfil
            };

            Entity newELE = new Entity(origOrbit.OwningEntity.Manager, planetDBs);
            return newELE;
        }
    }
}
