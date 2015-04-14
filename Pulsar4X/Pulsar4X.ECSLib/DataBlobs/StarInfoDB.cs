using System;

namespace Pulsar4X.ECSLib
{
    public enum SpectralType
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

    public enum LuminosityClass
    {
        O,          // Hypergiants
        Ia,         // Luminous Supergiants
        Iab,        // Intermediate Supergiants
        Ib,         // Less Luminos Supergiants
        II,         // Bright Giants
        III,        // Giants
        IV,         // Subgiants
        V,          // Main-Sequence (like our sun)
        sd,         // Subdwarfs
        D,          // White Dwarfs
    }

    class StarInfoDB : BaseDataBlob
    {
        public double Age { get; set; }
        public double Temperature { get; set; } // Effective ("Photosphere") temperature in Degrees C.
        public float Luminosity { get; set; }
        public string Class { get; set; }

        /// <summary>
        /// Calculates and sets the Habitable Zone of this star based on it Luminosity.
        /// calculated according to this site: http://www.planetarybiology.com/calculating_habitable_zone.html
        /// </summary>
        public double EcoSphereRadius { get { return (MinHabitableRadius + MaxHabitableRadius) / 2; } } // Average Habitable Radius, in AU.
        public double MinHabitableRadius { get { return Math.Sqrt(Luminosity / 1.1); } }  // in au
        public double MaxHabitableRadius { get { return Math.Sqrt(Luminosity / 0.53); } }  // in au

        public SpectralType SpectralType { get; set; }
        public ushort SpectralSubDivision { get; set; }       // number from  0 (hottest) to 9 (coolest)
        public LuminosityClass LuminosityClass { get; set; }
    }
}
