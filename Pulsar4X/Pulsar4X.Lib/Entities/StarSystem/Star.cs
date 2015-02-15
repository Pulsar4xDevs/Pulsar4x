using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Drawing;


namespace Pulsar4X.Entities
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
        O,
        I,
        II,
        III,
        IV,
        V,
        sd,
        D,
    }

    public class Star : OrbitingEntity
    {
        public BindingList<Planet> Planets { get; set; }
        public double Age { get; set; }
        public uint Temperature { get; set; } // Effective ("Photosphere") temperature in K.
        public float Luminosity { get; set; }
        public string Class { get; set; } // TODO: Use enums to generate.
        public double EcoSphereRadius { get; set; } // Average echo sphere. TODO: change this to include min and max radius from GetHabitableZone

        public Star()
            : base()
        {
            Planets = new BindingList<Planet>();
        }

        public Star(string name, double radius, uint temp, float luminosity, StarSystem system)
        {
            Name = name;
            Position.System = system;
            Position.X = 0;
            Position.Y = 0;
            Radius = radius;
            Temperature = temp;
            Luminosity = luminosity;

            Planets = new BindingList<Planet>();

            double minHabitableZone, maxHabitableZone;
            EcoSphereRadius = GetHabitableZone(out minHabitableZone, out maxHabitableZone);
        }

        /// <summary>
        /// Update the star's position and do any other work here
        /// </summary>
        /// <param name="deltaSeconds">Time to advance star position</param>
        public void UpdatePosition(int deltaSeconds)
        {
            Pulsar4X.Lib.OrbitTable.Instance.UpdatePosition(this, deltaSeconds);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="semiMajorAxis"></param>
        /// <param name="albedo"></param>
        /// <returns></returns>
        public uint GetEffectiveTemp(double semiMajorAxis, double albedo)
        {
            // <? todo: I'm pretty sure this code is wildly inaccurate.
            double S = (Radius / semiMajorAxis) * (2 * Constants.Science.σSB * Math.Pow(Temperature, 4));
            double Fa = S * (1 - albedo) / 4;
            uint Te = (uint)Math.Pow((Fa / Constants.Science.σSB), 0.25);
            return Te;
        }

        /// <summary>
        /// Calculates the Habitable Zone of this star.
        /// </summary>
        /// <param name="minRadius">Minimum Habitable Zone (Effective Temp == Water Boiling)</param>
        /// <param name="maxRadius">Maximum Habitable Zone (Effective Temp == Water Freezing)</param>
        /// <returns>Earth Habitable Zone (Effective Temp == 288K (15C))</returns>
        public double GetHabitableZone(out double minRadius, out double maxRadius)
        {
            uint TempAt1AU = GetEffectiveTemp(1, 0);
            minRadius = Math.Pow(Constants.Science.TEMP_WATER_BOIL / TempAt1AU, -2);
            maxRadius = Math.Pow(Constants.Science.TEMP_WATER_FREEZE / TempAt1AU, -2);
            return Math.Pow(288F / TempAt1AU, -2);
        }
    }
}
