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

    [TypeDescriptionProvider(typeof(StarTypeDescriptionProvider))]
    public class Star : OrbitingEntity
    {
        public BindingList<Planet> Planets { get; set; }
        public double Age { get; set; }
        public double Temperature { get; set; } // Effective ("Photosphere") temperature in Degrees C.
        public float Luminosity { get; set; }
        public string Class { get; set; }
        public double EcoSphereRadius { get; set; } // Average Habitable Radius, in AU.
        public double MinHabitableRadius { get; set; }  // in au
        public double MaxHabitableRadius { get; set; }  // in au

        public SpectralType SpectralType { get; set; }

        public Star()
            : base()
        {
            Planets = new BindingList<Planet>();
            SupportsPopulations = false;
        }

        public Star(string name, double radius, uint temp, float luminosity, SpectralType spectralType, StarSystem system)
        {
            Name = name;
            Position.System = system;
            Position.X = 0;
            Position.Y = 0;
            Temperature = temp;
            Luminosity = luminosity;
            SpectralType = spectralType;
            Radius = radius;

            Class = SpectralType.ToString();

            Planets = new BindingList<Planet>();
            SupportsPopulations = false;
        }

        /// <summary>
        /// Update the star's position and do any other work here
        /// </summary>
        /// <param name="deltaSeconds">Time to advance star position</param>
        public void UpdatePosition(int deltaSeconds)
        {
            double x, y;
            Orbit.GetPosition(GameState.Instance.CurrentDate, out x, out y);

            Position.X = x;
            Position.Y = y;

            if (Parent != null)
            {
                // Position is absolute system coordinates, while
                // coordinates returned from GetPosition are relative to it's parent.
                Position.X += Parent.Position.X;
                Position.Y += Parent.Position.Y;
            }

            foreach (Planet CurrentPlanet in Planets)
            {
                CurrentPlanet.UpdatePosition(deltaSeconds);
            }
        }
    }

    #region Data Binding

    /// <summary>
    /// Used for databinding, see here: http://blogs.msdn.com/b/msdnts/archive/2007/01/19/how-to-bind-a-datagridview-column-to-a-second-level-property-of-a-data-source.aspx
    /// </summary>
    public class StarTypeDescriptionProvider : TypeDescriptionProvider
    {
        private ICustomTypeDescriptor td;

        public StarTypeDescriptionProvider()
            : this(TypeDescriptor.GetProvider(typeof(Star)))
        { }

        public StarTypeDescriptionProvider(TypeDescriptionProvider parent)
            : base(parent)
        { }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            if (td == null)
            {
                td = base.GetTypeDescriptor(objectType, instance);
                td = new OrbitTypeDescriptor(td);
            }

            return td;
        }
    }

    #endregion
}
