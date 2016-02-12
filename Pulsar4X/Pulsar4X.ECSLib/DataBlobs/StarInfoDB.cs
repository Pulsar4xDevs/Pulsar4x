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
        public double Age
        {
            get { return _age; }
            internal set { _age = value; }
        }
        [JsonProperty]
        private double _age;

        // Effective ("Photosphere") temperature in Degrees C.
        [PublicAPI]
        public double Temperature
        {
            get { return _temperature; }
            internal set { _temperature = value; }
        }
        [JsonProperty]
        private double _temperature;

        [PublicAPI]
        public double Luminosity
        {
            get { return _luminosity; }
            internal set { _luminosity = value; }
        }
        [JsonProperty]
        private double _luminosity;

        [PublicAPI]
        public string Class
        {
            get { return _class; }
            internal set { _class = value; }
        }
        [JsonProperty]
        private string _class;



        [PublicAPI]
        public SpectralType SpectralType
        {
            get { return _spectralType; }
            internal set { _spectralType = value; }
        }
        [JsonProperty]
        private SpectralType _spectralType;

        // number from  0 (hottest) to 9 (coolest)
        [PublicAPI]
        public ushort SpectralSubDivision
        {
            get { return _spectralSubDivision; }
            internal set { _spectralSubDivision = value; }
        }
        [JsonProperty]
        private ushort _spectralSubDivision;

        [PublicAPI]
        public LuminosityClass LuminosityClass
        {
            get { return _luminosityClass; }
            internal set { _luminosityClass = value; }
        }
        [JsonProperty]
        private LuminosityClass _luminosityClass;

        /// <summary>
        /// Calculates and sets the Habitable Zone of this star based on it Luminosity.
        /// calculated according to this site: http://www.planetarybiology.com/calculating_habitable_zone.html
        /// </summary>
        [PublicAPI]
        public double EcoSphereRadius { get { return (MinHabitableRadius + MaxHabitableRadius) / 2; } } // Average Habitable Radius, in AU.
        [PublicAPI]
        public double MinHabitableRadius { get { return Math.Sqrt(Luminosity / 1.1); } }  // in au
        [PublicAPI]
        public double MaxHabitableRadius { get { return Math.Sqrt(Luminosity / 0.53); } }  // in au

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
