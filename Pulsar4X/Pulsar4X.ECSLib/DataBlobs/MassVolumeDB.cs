using Newtonsoft.Json;
using System;

namespace Pulsar4X.ECSLib
{
    public class MassVolumeDB : BaseDataBlob, ISensorCloneMethod, IGetValuesHash
    {

        /// <summary>
        /// Mass in KG of this entity.
        /// </summary>
        [JsonProperty]
        public double Mass { get; internal set; }

        /// <summary>
        /// Volume_km3 of this entity in Km^3.
        /// </summary>
        [JsonProperty]
        public double Volume_km3 { get; internal set; }

        /// <summary>
        /// Gets the volume in m^3.
        /// </summary>
        /// <value>The volume m^3.</value>
        [JsonIgnore]
        public double Volume_m3 { 
            get { return Volume_km3 * 1e9; }
            set { Volume_km3 = value / 1e9; }
        }

        /// <summary>
        /// The density of the body in g/cm^3
        /// </summary> 
        [JsonProperty]
        public double Density { get; internal set; }

        /// <summary>
        /// The Average Radius in AU.
        /// </summary>
        [JsonProperty]
        public double RadiusInAU
        {
            get { return Distance.MToAU(RadiusInM); }
            set { RadiusInM = Distance.AuToMt(value); }
        }

        /// <summary>
        /// The Average Radius in Km.
        /// </summary>
        public double RadiusInKM
        {
            get { return RadiusInM * 1000; }
            internal set { RadiusInM = value * 0.001; }
        }

        public double RadiusInM { get; internal set; }

        /// <summary>
        /// Measure on the gravity of a planet at its surface.
        /// In Earth Gravities (Gs).
        /// </summary>
        public double SurfaceGravity => GMath.GetStandardGravitationAttraction(Mass, RadiusInKM * 1000);

        public MassVolumeDB()
        {
        }

        /// <summary>
        /// Generates a new MassVolumeDB from mass and radius_au, calculating density and volume.
        /// </summary>
        /// <param name="mass">Mass in Kg.</param>
        /// <param name="radius_au">Radius in AU</param>
        /// <returns></returns>
        internal static MassVolumeDB NewFromMassAndRadius_AU(double mass, double radius_au)
        {
            var mvDB = new MassVolumeDB {Mass = mass, RadiusInAU = radius_au, Volume_km3 = CalculateVolume_Km3(radius_au)};
            mvDB.Density = CalculateDensity(mass, mvDB.Volume_km3);

            return mvDB;
        }
        
        internal static MassVolumeDB NewFromMassAndRadius_m(double mass, double radius_m)
        {
            var mvDB = new MassVolumeDB {Mass = mass, RadiusInM = radius_m, Volume_m3 = CalculateVolume_m3(radius_m)};
            mvDB.Density = CalculateDensity(mass, mvDB.Volume_km3);

            return mvDB;
        }

        /// <summary>
        /// Generates a n ew MassVolumeDB from mass and density, calculating radius_au and volume.
        /// </summary>
        /// <param name="mass">Mass in Kg</param>
        /// <param name="density">Density in Kg/cm^3</param>
        /// <returns></returns>
        internal static MassVolumeDB NewFromMassAndDensity(double mass, double density)
        {
            var mvDB = new MassVolumeDB {Mass = mass, Density = density, Volume_km3 = CalculateVolume_Km3_FromMassAndDesity(mass, density), RadiusInAU = CalculateRadius_Au(mass, density)};

            return mvDB;
        }

        /// <summary>
        /// Generates a new MassVolumeDB from mass and volume, calculating deinsity and radius_au.
        /// </summary>
        /// <param name="mass">Mass in Kg</param>
        /// <param name="volume">Density in m^3</param>
        /// <returns></returns>
        internal static MassVolumeDB NewFromMassAndVolume(double mass, double volume)
        {
            var density = CalculateDensity(mass, volume);
            var rad = CalculateRadius_Au(mass, density);
            var mvDB = new MassVolumeDB { Mass = mass, Volume_km3 = volume, Density = density, RadiusInAU = rad };

            return mvDB;
        }

        public MassVolumeDB(MassVolumeDB massVolumeDB)
        {
            Mass = massVolumeDB.Mass;
            Density = massVolumeDB.Density;
            RadiusInAU = massVolumeDB.RadiusInAU;
            Volume_km3 = massVolumeDB.Volume_km3;
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
        /// <returns>Volume_km3 in Km^3</returns>
        public static double CalculateVolume_Km3_FromMassAndDesity(double mass, double density)
        {
            double volumeInCm3 = mass / density;

            // now return after converting to Km^3
            return volumeInCm3 * 1.0e-15;
        }

        /// <summary>
        /// Calculates volume in Km^3 from a radius in Au.
        /// </summary>
        /// <param name="radius">Radius in AU</param>
        /// <returns>Volume_km3 in Km^3</returns>
        public static double CalculateVolume_Km3(double radius_au)
        {
            return (4.0 / 3.0) * Math.PI * Math.Pow(Distance.AuToKm(radius_au), 3);
        }

        /// <summary>
        /// Calculates volume in m^3 from a radius of meters
        /// </summary>
        /// <param name="radius_m"></param>
        /// <returns></returns>
        public static double CalculateVolume_m3(double radius_m)
        {
            return (4.0 / 3.0) * Math.PI * Math.Pow(radius_m, 3);
        }
        
        /// <summary>
        /// Calculate density from mass and volume
        /// </summary>
        /// <param name="mass">Mass in Kg</param>
        /// <param name="volume">Volume_km3 in Km^3</param>
        /// <returns>Density in g/cm^3</returns>
        public static double CalculateDensity(double mass, double volume)
        {
            double densityinkg_m3 = mass / (volume * 1.0e9); // convert volume to meters cube now to make later conversions eaiser.

            // now convert to g/cm^3
            return densityinkg_m3 * 0.001;
        }

        /// <summary>
        /// Calculates the radius_au of a body from mass and densitiy using the formular: 
        /// <c>r = ((3M)/(4pD))^(1/3)</c>
        /// Where p = PI, D = Density, and M = Mass.
        /// </summary>
        /// <param name="mass">The mass of the body in Kg</param>
        /// <param name="density">The density in g/cm^2</param>
        /// <returns>The radius_au in AU</returns>
        public static double CalculateRadius_Au(double mass, double density)
        {
            double radius = Math.Pow((3 * mass) / (4 * Math.PI * (density / 1000)), 0.3333333333); // density / 1000 changes it from g/cm2 to Kg/cm3, needed because mass in is KG. 
            // 0.3333333333 should be 1/3 but 1/3 gives radius_au of 0.999999 for any mass/density pair, so i used 0.3333333333
            return Distance.KmToAU(radius / 1000 / 100);     // convert from cm to AU.
        }

        public BaseDataBlob SensorClone(SensorInfoDB sensorInfo)
        {
            return new MassVolumeDB(this, sensorInfo);
        }

        MassVolumeDB(MassVolumeDB massVolumeDB, SensorInfoDB sensorInfo)
        {
            Update(massVolumeDB, sensorInfo);
        }

        public void SensorUpdate(SensorInfoDB sensorInfo)
        {
            Update(sensorInfo.DetectedEntity.GetDataBlob<MassVolumeDB>(), sensorInfo);
        }

        void Update(MassVolumeDB origionalDB, SensorInfoDB sensorInfo)
        {
            //TODO: add rand from sensorInfo. 
            Mass = origionalDB.Mass;
            Density = origionalDB.Density;
            RadiusInAU = origionalDB.RadiusInAU;
            Volume_km3 = origionalDB.Volume_km3;
        }

        public int GetValueCompareHash(int hash = 17)
        {
            hash = Misc.ValueHash(Mass, hash);
            hash = Misc.ValueHash(Density);
            hash = Misc.ValueHash(RadiusInAU);
            hash = Misc.ValueHash(Volume_km3);
            return hash;
        }
    }
}
