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
    public class Star : OrbitingEntity
    {
        double XGalactic { get; set; }
        double YGalactic { get; set; }
        double ZGalactic { get; set; }

        public override double Mass { get { return m_dMass; } set { m_dMass = value; } }

        //public double Luminosity { get; set; }
        public double Life { get; set; }
        public override double Age { get; set; }
        public double Temperature { get; set; }
        //public double Radius { get; set; }
        public Color Color { get; set; }
        
        //public double EcoSphereRadius { get; set; }
        public int SpectrumAdjustment { get; set; }
        public StarSpectrum Spectrum { get; set; }

        //public double OrbitalRadius { get; set; }
        public double EcoSphereRadius { get; set; }
        public double Luminosity { get; set; }
        
        public BindingList<Planet> Planets { get; set; }
        public StarSystem StarSystem { get; set; }

        [JsonIgnore]
        public string Class
        {
            get
            {
                return (Spectrum.ToString() + SpectrumAdjustment.ToString());
            }
        }

        public Star() : base()
        {
            Planets = new BindingList<Planet>();
        }

        /// <summary>
        /// Update the star's position and do any other work here
        /// </summary>
        /// <param name="tickValue">Time to advance star position</param>
        public void UpdatePosition(int tickValue)
        {
            Pulsar4X.Lib.OrbitTable.Instance.UpdatePosition(this, tickValue);
        }
    }
}
