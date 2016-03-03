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

    public class StarInfoDB : BaseDataBlob
    {
        [PublicAPI]
        [JsonProperty]
        public double Age { get; internal set; }

        // Effective ("Photosphere") temperature in Degrees C.
        [PublicAPI]
        [JsonProperty]
        public double Temperature { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public double Luminosity { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public string Class { get; internal set; }


        [PublicAPI]
        [JsonProperty]
        public SpectralType SpectralType { get; internal set; }

        // number from  0 (hottest) to 9 (coolest)
        [PublicAPI]
        [JsonProperty]
        public ushort SpectralSubDivision { get; internal set; }

        [PublicAPI]
        [JsonProperty]
        public LuminosityClass LuminosityClass { get; internal set; }

        /// <summary>
        /// Calculates and sets the Habitable Zone of this star based on it Luminosity.
        /// calculated according to this site: http://www.planetarybiology.com/calculating_habitable_zone.html
        /// </summary>
        [PublicAPI]
        public double EcoSphereRadius => (MinHabitableRadius + MaxHabitableRadius) / 2;

        // Average Habitable Radius, in AU.
        [PublicAPI]
        public double MinHabitableRadius => Math.Sqrt(Luminosity / 1.1);

        // in au
        [PublicAPI]
        public double MaxHabitableRadius => Math.Sqrt(Luminosity / 0.53);

        // in au

        public StarInfoDB()
        {
            
        }

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
    }
}
