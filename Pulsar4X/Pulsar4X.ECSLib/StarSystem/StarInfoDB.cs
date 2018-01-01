using Newtonsoft.Json;
using System;

namespace Pulsar4X.ECSLib
{
    public enum SpectralType : byte
    {
        O,
        B,
        A,
        F,
        G,
        K,
        M,
        D,
        C
    }

    public enum LuminosityClass : byte
    {
        O,          // Hypergiants
        Ia,         // Luminous Supergiants
        Iab,        // Intermediate Supergiants
        Ib,         // Less Luminous Supergiants
        II,         // Bright Giants
        III,        // Giants
        IV,         // Subgiants
        V,          // Main-Sequence (like our sun)
        sd,         // Subdwarfs
        D,          // White Dwarfs
    }

    public class StarInfoDB : BaseDataBlob, ISensorCloneMethod
    {
        /// <summary>
        /// Age of this star. Fluff.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double Age { get; internal set; }

        /// <summary>
        /// Effective ("Photosphere") temperature in Degrees C.
        /// Affects habitable zone.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double Temperature { get; internal set; }

        /// <summary>
        /// Luminosity of this star. Fluff.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public double Luminosity { get; internal set; }

        /// <summary>
        /// Star class. Mostly fluff (affects SystemGeneration).
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public string Class { get; internal set; }

        /// <summary>
        /// Main Type. Mostly fluff (affects SystemGeneration).
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public SpectralType SpectralType { get; internal set; }

        /// <summary>
        /// Subtype.  Mostly fluff (affects SystemGeneration).
        /// number from  0 (hottest) to 9 (coolest)
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public ushort SpectralSubDivision { get; internal set; }

        /// <summary>
        /// LuminosityClass. Fluff.
        /// </summary>
        [PublicAPI]
        [JsonProperty]
        public LuminosityClass LuminosityClass { get; internal set; }

        /// <summary>
        /// Calculates and sets the Habitable Zone of this star based on it Luminosity.
        /// calculated according to this site: http://www.planetarybiology.com/calculating_habitable_zone.html
        /// </summary>
        [PublicAPI]
        public double EcoSphereRadius => (MinHabitableRadius + MaxHabitableRadius) / 2;

        /// <summary>
        /// Minimum edge of the Habitable Zone (in AU)
        /// </summary>
        [PublicAPI]
        public double MinHabitableRadius => Math.Sqrt(Luminosity / 1.1);

        /// <summary>
        /// Maximum edge of the Habitable Zone (in AU)
        /// </summary>
        [PublicAPI]
        public double MaxHabitableRadius => Math.Sqrt(Luminosity / 0.53);
        
        public StarInfoDB() { }

        public StarInfoDB(StarInfoDB starInfoDB)
        {
            Age = starInfoDB.Age;
            Temperature = starInfoDB.Temperature;
            Luminosity = starInfoDB.Luminosity;
            Class = starInfoDB.Class;

            SpectralType = starInfoDB.SpectralType;
            SpectralSubDivision = starInfoDB.SpectralSubDivision;
            LuminosityClass = starInfoDB.LuminosityClass;

        }

        public override object Clone()
        {
            return new StarInfoDB(this);
        }

        public BaseDataBlob SensorClone(SensorInfoDB sensorInfo)
        {
            return new StarInfoDB(this, sensorInfo);
        }

        StarInfoDB(StarInfoDB db, SensorInfoDB sensorInfo)
        {
            Update(db, sensorInfo);

        }

        public void SensorUpdate(SensorInfoDB sensorInfo)
        {
            Update(sensorInfo.DetectedEntity.GetDataBlob<StarInfoDB>(), sensorInfo);
        }

        void Update(StarInfoDB db, SensorInfoDB sensorInfo)
        {
            Random rng = new Random();
            float accuracy = sensorInfo.HighestDetectionQuality.SignalQuality;

            Age = SensorProcessorTools.RndSigmoid(db.Age, accuracy, rng);
            Temperature = SensorProcessorTools.RndSigmoid(db.Temperature, accuracy, rng);
            Luminosity = SensorProcessorTools.RndSigmoid(db.Luminosity, accuracy, rng);
            Class = db.Class;

            SpectralType = db.SpectralType;
            SpectralSubDivision = db.SpectralSubDivision;
            LuminosityClass = db.LuminosityClass;
        }
    }
}
