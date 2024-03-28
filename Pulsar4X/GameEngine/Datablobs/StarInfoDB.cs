using Newtonsoft.Json;
using System;
using Pulsar4X.Orbital;
using System.ComponentModel;
using Pulsar4X.DataStructures;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;
using System.Collections.Generic;

namespace Pulsar4X.Datablobs
{
    public class StarInfoDB : BaseDataBlob, ISensorCloneMethod
    {
        public new static List<Type> GetDependencies() => new List<Type>() { typeof(NameDB), typeof(MassVolumeDB) };

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
        public double EcoSphereRadius_AU => (MinHabitableRadius_AU + MaxHabitableRadius_AU) / 2;

        public double EcoSphereRadius_m => (MinHabitableRadius_m + MaxHabitableRadius_m) / 2;
        /// <summary>
        /// Minimum edge of the Habitable Zone (in AU)
        /// </summary>
        [PublicAPI]
        public double MinHabitableRadius_AU => Math.Sqrt(Luminosity / 1.1);

        public double MinHabitableRadius_m => Distance.AuToMt(MinHabitableRadius_AU);
        /// <summary>
        /// Maximum edge of the Habitable Zone (in AU)
        /// </summary>
        [PublicAPI]
        public double MaxHabitableRadius_AU => Math.Sqrt(Luminosity / 0.53);

        public double MaxHabitableRadius_m => Distance.AuToMt(MaxHabitableRadius_AU);

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

            Age = SensorTools.RndSigmoid(db.Age, accuracy, rng);
            Temperature = SensorTools.RndSigmoid(db.Temperature, accuracy, rng);
            Luminosity = SensorTools.RndSigmoid(db.Luminosity, accuracy, rng);
            Class = db.Class;

            SpectralType = db.SpectralType;
            SpectralSubDivision = db.SpectralSubDivision;
            LuminosityClass = db.LuminosityClass;
        }
    }
}
