using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
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
            Vector4 velocity = new Vector4(8, 7, 0, 0);

            var position = new PositionDB(0, 0, 0, Guid.Empty);
            var massVolume = MassVolumeDB.NewFromMassAndRadius(mass, radius);
            var planetInfo = new SystemBodyInfoDB();
            var balisticTraj = new NewtonBalisticDB(target.Guid,collisionDate);
            var name = new NameDB("Ellie");
            var AsteroidDmg = new AsteroidDamageDB();
            var sensorPfil = new SensorProfileDB();

            planetInfo.SupportsPopulations = false;
            planetInfo.BodyType = BodyType.Asteroid;

            Vector4 targetPos = OrbitProcessor.GetAbsolutePosition(target.GetDataBlob<OrbitDB>(), collisionDate);
            TimeSpan timeToCollision = collisionDate - starSys.Game.CurrentDateTime;
            Vector4 offset = velocity * timeToCollision.TotalSeconds;
            targetPos -=  Distance.KmToAU(offset);
            position.AbsolutePosition = targetPos;
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

            Entity newELE = new Entity(starSys.SystemManager, planetDBs);
            return newELE;
        }
    }
}
