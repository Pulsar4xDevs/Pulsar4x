using System;

namespace Pulsar4X.ECSLib
{
    public class MassVolumeDB : BaseDataBlob
    {
        /// <summary>
        /// Mass in KG of this entity.
        /// </summary>
        public double Mass { get; set; }

        /// <summary>
        /// Volume of this entity.
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// The density of the body in kg/cm^3
        /// </summary> 
        public double Density
        {
            get { return GetDensity(Mass, Volume); }
        }

        /// <summary>
        /// The Average Radius
        /// </summary>
        public double Radius
        {
            get { return GetRadius(Volume); }
        }

        /// <summary>
        /// Measure on the gravity of a planet at its surface.
        /// In Earth Gravities (Gs).
        /// </summary>
        public double SurfaceGravity
        {
            get { return GMath.GetStandardGravitationAttraction(Mass, Radius); }
        }

        public MassVolumeDB()
        {
        }

        public MassVolumeDB(double mass, double volume)
        {
            Mass = mass;
            Volume = volume;
        }

        public MassVolumeDB(MassVolumeDB massVolumeDB)
            :this(massVolumeDB.Mass, massVolumeDB.Volume)
        {
        }

        public override object Clone()
        {
            return new MassVolumeDB(this);
        }

        public static double GetMass(double volume, double density)
        {
            return density * volume;
        }

        public static double GetVolume(double mass, double density)
        {
            return mass / density;
        }

        public static double GetDensity(double mass, double volume)
        {
            return mass / volume;
        }

        public static double GetRadius(double volume)
        {
            // v = (4pi r^3)/3
            // v = 4/3pi * r^3
            // r^3 = V / (4/3pi)
            // r = (V / (4/3pi)) ^ (1/3)
            return Math.Pow(volume / (4/3 * Math.PI), (1/3));
        }
    }
}
