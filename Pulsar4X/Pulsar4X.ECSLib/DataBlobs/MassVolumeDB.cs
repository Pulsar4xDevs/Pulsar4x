using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class MassVolumeDB : BaseDataBlob
    {

        /// <summary>
        /// Mass in KG of this entity.
        /// </summary>
        public double Mass
        {
            get { return _mass; }
            internal set { _mass = value; }
        }
        [JsonProperty]
        private double _mass;

        /// <summary>
        /// Volume of this entity in Km^3.
        /// </summary>
        public double Volume
        {
            get { return _volume; }
            internal set { _volume = value; }
        }
        [JsonProperty]
        private double _volume;

        /// <summary>
        /// The density of the body in kg/cm^3
        /// </summary> 
        public double Density
        {
            get { return _density; }
            internal set { _density = value; }
        }
        [JsonProperty]
        private double _density;

        /// <summary>
        /// The Average Radius in AU.
        /// </summary>
        [JsonIgnore]
        public double Radius
        {
            get { return _radius; }
            internal set { _radius = value; }
        }
        [JsonProperty]
        private double _radius;

        /// <summary>
        /// The Average Radius in Km.
        /// </summary>
        public double RadiusInKM
        {
            get { return Distance.ToKm(Radius); }
            internal set { Radius = Distance.ToAU(Radius); }
        }

        /// <summary>
        /// Measure on the gravity of a planet at its surface.
        /// In Earth Gravities (Gs).
        /// </summary>
        public double SurfaceGravity
        {
            get { return GMath.GetStandardGravitationAttraction(Mass, RadiusInKM * 1000); }  // radius needs to be i meters here.
        }

        public MassVolumeDB()
        {
        }

        /// <summary>
        /// Generates a new MassVolumeDB from mass and radius, calculating density and volume.
        /// </summary>
        /// <param name="mass">Mass in Kg.</param>
        /// <param name="radius">Radius in AU</param>
        /// <returns></returns>
        internal static MassVolumeDB NewFromMassAndRadius(double mass, double radius)
        {
            MassVolumeDB mvDB = new MassVolumeDB();
            mvDB.Mass = mass;
            mvDB.Radius = radius;
            mvDB.Volume = CalculateVolume(radius);
            mvDB.Density = CalculateDensity(mass, mvDB.Volume);

            return mvDB;
        }

        /// <summary>
        /// Generates a n ew MassVolumeDB from mass and density, calculating radius and volume.
        /// </summary>
        /// <param name="mass">Mass in Kg</param>
        /// <param name="density">Density in Kg/cm^3</param>
        /// <returns></returns>
        internal static MassVolumeDB NewFromMassAndDensity(double mass, double density)
        {
            MassVolumeDB mvDB = new MassVolumeDB();
            mvDB.Mass = mass;
            mvDB.Density = density;
            mvDB.Volume = CalculateVolume(mass, density);
            mvDB.Radius = CalculateRadius(mass, density);

            return mvDB;
        }

        public MassVolumeDB(MassVolumeDB massVolumeDB)
        {
            Mass = massVolumeDB.Mass;
            Density = massVolumeDB.Density;
            Radius = massVolumeDB.Radius;
            Volume = massVolumeDB.Volume;
        }

        public override object Clone()
        {
            return new MassVolumeDB(this);
        }

        public static double CalculateMass(double volume, double density)
        {
            return density * volume;
        }

        /// <summary>
        /// Calculates the volume given mass and density.
        /// </summary>
        /// <param name="mass">Mass in Kg</param>
        /// <param name="density">Density in Kg/cm^3</param>
        /// <returns>Volume in Km^3</returns>
        public static double CalculateVolume(double mass, double density)
        {
            double volumeInCm3 = mass / density;

            // now return after converting to Km^3
            return volumeInCm3 * 1.0e-15;
        }

        /// <summary>
        /// Calculates volume from a radius.
        /// </summary>
        /// <param name="radius">Radius in AU</param>
        /// <returns>Volume in Km^3</returns>
        public static double CalculateVolume(double radius)
        {
            return (4.0 / 3.0) * Math.PI * Math.Pow(Distance.ToKm(radius), 3);
        }

        /// <summary>
        /// Calculate density from mass and volume
        /// </summary>
        /// <param name="mass">Mass in Kg</param>
        /// <param name="volume">Volume in Km^3</param>
        /// <returns>Density in g/cm^3</returns>
        public static double CalculateDensity(double mass, double volume)
        {
            double volumeInM3 = mass / (volume / 1.0e-9); // convert volume to meters cube now to make later conversions eaiser.

            // now convert to g/cm^3
            return volumeInM3 * 0.001;
        }

        /// <summary>
        /// Calculates the radius of a body from mass and densitiy using the formular: 
        /// <c>r = ((3M)/(4pD))^(1/3)</c>
        /// Where p = PI, D = Density, and M = Mass.
        /// </summary>
        /// <param name="mass">The mass of the body in Kg</param>
        /// <param name="density">The density in g/cm^2</param>
        /// <returns>The radius in AU</returns>
        public static double CalculateRadius(double mass, double density)
        {
            double radius = Math.Pow((3 * mass) / (4 * Math.PI * (density / 1000)), 0.3333333333); // density / 1000 changes it from g/cm2 to Kg/cm3, needed because mass in is KG. 
            // 0.3333333333 should be 1/3 but 1/3 gives radius of 0.999999 for any mass/density pair, so i used 0.3333333333
            return Distance.ToAU(radius / 1000 / 100);     // convert from cm to AU.
        }
    }
}
