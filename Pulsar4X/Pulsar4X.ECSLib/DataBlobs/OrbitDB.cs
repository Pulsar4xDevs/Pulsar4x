﻿using System;

namespace Pulsar4X.ECSLib
{
    public class OrbitDB : BaseDataBlob
    {
        /// <summary>
        /// Mass in KG of this entity.
        /// </summary>
        public double Mass;

        /// <summary>
        /// Mass in KG of parent (object this orbit orbits)
        /// </summary>
        public double ParentMass;

        /// <summary>
        /// Semimajor Axis of orbit stored in AU.
        /// Average distance of orbit from center.
        /// </summary>
        public double SemiMajorAxis;

        /// <summary>
        /// Eccentricity of orbit.
        /// Shape of the orbit. 0 = perfectly circular, 1 = parabolic.
        /// </summary>
        public double Eccentricity;

        /// <summary>
        /// Angle between the orbit and the flat referance plane.
        /// Stored in degrees.
        /// </summary>
        public double Inclination;

        /// <summary>
        /// Horizontal orientation of the point where the orbit crosses
        /// the referance frame stored in degrees.
        /// </summary>
        public double LongitudeOfAscendingNode;

        /// <summary>
        /// Angle from the Ascending Node to the Periapsis stored in degrees.
        /// </summary>
        public double ArgumentOfPeriapsis;

        /// <summary>
        /// Definition of the position of the body in the orbit at the referance time
        /// epoch. Mathematically convienant angle does not correspond to a real angle.
        /// Stored in degrees.
        /// </summary>
        public double MeanAnomaly;

        /// <summary>
        /// Referance time. Orbital parameters are stored relative to this referance.
        /// </summary>
        public DateTime Epoch;

        /// <summary>
        /// 2-Body gravitational parameter of system.
        /// </summary>
        public double GravitationalParameter;

        /// <summary>
        /// Orbital Period of orbit.
        /// </summary>
        public TimeSpan OrbitalPeriod;

        /// <summary>
        /// Mean Motion of orbit. Stored as Degrees/Sec.
        /// </summary>
        public double MeanMotion;

        /// <summary>
        /// Point in orbit furthest from the ParentBody. Measured in AU.
        /// </summary>
        public double Apoapsis;

        /// <summary>
        /// Point in orbit closest to the ParentBody. Measured in AU.
        /// </summary>
        public double Periapsis;

        /// <summary>
        /// Stationary orbits don't have all of the data to update. They always return (0, 0).
        /// </summary>
        public bool IsStationary;

        #region Construction Interface
        /// <summary>
        /// Returns an orbit representing the defined parameters.
        /// </summary>
        /// <param name="mass">Mass of this object in KG.</param>
        /// <param name="parentMass">Mass of parent object in KG.</param>
        /// <param name="semiMajorAxis">SemiMajorAxis of orbit in AU.</param>
        /// <param name="eccentricity">Eccentricity of orbit.</param>
        /// <param name="inclination">Inclination of orbit in degrees.</param>
        /// <param name="longitudeOfAscendingNode">Longitude of ascending node in degrees.</param>
        /// <param name="longitudeOfPeriapsis">Longitude of periapsis in degrees.</param>
        /// <param name="meanLongitude">Longitude of object at epoch in degrees.</param>
        /// <param name="epoch">Referance time for these orbital elements.</param>
        public static OrbitDB FromMajorPlanetFormat(Guid parentGuid, double mass, double parentMass, double semiMajorAxis, double eccentricity, double inclination,
                                                    double longitudeOfAscendingNode, double longitudeOfPeriapsis, double meanLongitude, DateTime epoch)
        {
            // http://en.wikipedia.org/wiki/Longitude_of_the_periapsis
            double argumentOfPeriapsis = longitudeOfPeriapsis - longitudeOfAscendingNode;
            // http://en.wikipedia.org/wiki/Mean_longitude
            double meanAnomaly = meanLongitude - (longitudeOfAscendingNode + argumentOfPeriapsis);

            return new OrbitDB(parentGuid, mass, parentMass, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch, false);
        }

        /// <summary>
        /// Returns an orbit representing the defined parameters.
        /// </summary>
        /// <param name="mass">Mass of this object in KG.</param>
        /// <param name="parentMass">Mass of parent object in KG.</param>
        /// <param name="semiMajorAxis">SemiMajorAxis of orbit in AU.</param>
        /// <param name="eccentricity">Eccentricity of orbit.</param>
        /// <param name="inclination">Inclination of orbit in degrees.</param>
        /// <param name="longitudeOfAscendingNode">Longitude of ascending node in degrees.</param>
        /// <param name="argumentOfPeriapsis">Argument of periapsis in degrees.</param>
        /// <param name="meanAnomaly">Mean Anomaly in degrees.</param>
        /// <param name="epoch">Referance time for these orbital elements.</param>
        public static OrbitDB FromAsteroidFormat(Guid parentGuid, double mass, double parentMass, double semiMajorAxis, double eccentricity, double inclination,
                                                double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch)
        {
            return new OrbitDB(parentGuid, mass, parentMass, semiMajorAxis, eccentricity, inclination, longitudeOfAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch, false);
        }

        /// <summary>
        /// Creates an orbit that never moves.
        /// </summary>
        public static OrbitDB FromStationary(double mass)
        {
            return new OrbitDB(mass);
        }

        public OrbitDB(OrbitDB orbitDB) : this(Guid.Empty, orbitDB.Mass, orbitDB.ParentMass, orbitDB.SemiMajorAxis, orbitDB.Eccentricity, orbitDB.Inclination,
            orbitDB.LongitudeOfAscendingNode, orbitDB.ArgumentOfPeriapsis, orbitDB.MeanAnomaly, orbitDB.Epoch, orbitDB.IsStationary)
        {
        }

        private OrbitDB(double mass)
        {
            Mass = mass;
            IsStationary = true;
        }

        private OrbitDB(Guid parentGuid, double mass, double parentMass, double semiMajorAxis, double eccentricity, double inclination,
                        double longitudeOfAscendingNode, double argumentOfPeriapsis, double meanAnomaly, DateTime epoch, bool isStationary)
        {
            if (isStationary)//Most worst solution. Added to be able to use constructor OrbitDB(OrbitDB) 
            {
                Mass = mass;
                IsStationary = true;
            }

            Mass = mass;
            ParentMass = parentMass;
            SemiMajorAxis = semiMajorAxis;
            Eccentricity = Math.Min(eccentricity, 0.8D); // Max eccentricity is 0.8 Orbit code has issues at higher eccentricity. (Note: If restriction lifed, fix code in GetEccentricAnomaly)
            Inclination = inclination;
            LongitudeOfAscendingNode = longitudeOfAscendingNode;
            ArgumentOfPeriapsis = argumentOfPeriapsis;
            MeanAnomaly = meanAnomaly;
            Epoch = epoch;
            IsStationary = false;
            
            // Calculate extended parameters.
            // http://en.wikipedia.org/wiki/Standard_gravitational_parameter#Two_bodies_orbiting_each_other
            GravitationalParameter = GameSettings.Science.GravitationalConstant * (ParentMass + Mass) / (1000 * 1000 * 1000); // Normalize GravitationalParameter from m^3/s^2 to km^3/s^2

            // http://en.wikipedia.org/wiki/Orbital_period#Two_bodies_orbiting_each_other
            double orbitalPeriod = 2 * Math.PI * Math.Sqrt(Math.Pow(Distance.ToKm(SemiMajorAxis), 3) / (GravitationalParameter));
            if (orbitalPeriod * 10000000 > Int64.MaxValue)
            {
                OrbitalPeriod = TimeSpan.MaxValue;
            }
            else
            {
                OrbitalPeriod = TimeSpan.FromSeconds(orbitalPeriod);
            }

            // http://en.wikipedia.org/wiki/Mean_motion
            MeanMotion = Math.Sqrt(GravitationalParameter / Math.Pow(Distance.ToKm(SemiMajorAxis), 3)); // Calculated in radians.
            MeanMotion = Angle.ToDegrees(MeanMotion); // Stored in degrees.

            Apoapsis = (1 + Eccentricity) * SemiMajorAxis;
            Periapsis = (1 - Eccentricity) * SemiMajorAxis;
        }
        #endregion
    }
}
