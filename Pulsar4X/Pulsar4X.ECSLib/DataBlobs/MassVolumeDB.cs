using System;

namespace Pulsar4X.ECSLib
{
    public class MassVolumeDB : BaseDataBlob
    {
        /// <summary>
        /// Mass in KG of this entity.
        /// </summary>
        public double Mass { get; set; }

        /// <summary>
        /// The density of the body in g/cm^3
        /// </summary> 
        public double Density { get; set; }

        /// <summary>
        /// The Average Radius (in AU)
        /// </summary>
        public double Radius
        {
            get
            {
                // r = ((3M)/(4pD))^(1/3)
                // Where p = PI, D = Density, and M = Mass.
                // density / 1000 changes it from g/cm2 to Kg/cm3, needed because mass in is KG. 
                // 0.3333333333 should be 1/3 but 1/3 gives radius of 0.999999 for any mass/density pair, so i used 0.3333333333
                return Distance.ToAU(Math.Pow((3 * Mass) / (4 * Math.PI * (Density / 1000)), 0.3333333333) / 1000 / 100); // convert from cm to AU.
            }
        }

        /// <summary>
        /// The Average Radius (in km)
        /// </summary>
        public double RadiusinKM
        {
            get { return Distance.ToKm(Radius); }
        }

        /// <summary>
        /// Measure on the gravity of a planet at its surface.
        /// In Earth Gravities (Gs).
        /// </summary>
        public float SurfaceGravity
        {
            get
            {
                // see: http://nova.stanford.edu/projects/mod-x/ad-surfgrav.html
                return (float)((GameSettings.Science.GravitationalConstant * Mass) / (Radius * GameSettings.Units.MetersPerAu) * (Radius * GameSettings.Units.MetersPerAu));
            }
        }

        public MassVolumeDB()
        {
        }

        public MassVolumeDB(double mass, double density)
        {
            Mass = mass;
            Density = density;
        }

        public MassVolumeDB(MassVolumeDB massVolumeDB)
            :this(massVolumeDB.Mass, massVolumeDB.Density)
        {
            
        }

        public override object Clone()
        {
            return new MassVolumeDB(this);
        }
    }
}
