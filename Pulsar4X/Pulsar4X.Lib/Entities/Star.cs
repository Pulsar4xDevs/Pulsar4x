using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Pulsar4X.Entities
{
    public class Star
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public long XGalactic { get; set; }
        public long YGalactic { get; set; }
        public long XSystem { get; set; }
        public long YSystem { get; set; }

        public double Luminosity { get; set; }
        public double Mass { get; set; }
        public double Life { get; set; }
        public double Age { get; set; }
        public double EcoSphereRadius { get; set; }
        public int SpectrumAdjustment { get; set; }
        public StarSpectrum Spectrum { get; set; }

        public BindingList<Planet> Planets { get; set; }
        public Guid StarSystemId { get; set; }
        public StarSystem StarSystem { get; set; }

        public string Class
        {
            get
            {
                return (Spectrum.ToString() + SpectrumAdjustment.ToString());
            }
        }

        public Star()
        {
            Planets = new BindingList<Planet>();
        }
    }
}
