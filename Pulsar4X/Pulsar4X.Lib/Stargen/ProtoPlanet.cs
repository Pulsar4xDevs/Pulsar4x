namespace Pulsar4X.Stargen
{
    public class ProtoPlanet
    {
        public double SemiMajorAxis { get; set; }
        public double Eccentricity { get; set; }
        public double Mass { get; set; }
        public double DustMass { get; set; }
        public double GasMass { get; set; }
        //public double MassToCollectGas { get; set; }

        public ProtoPlanet(double a, double e, double mass)
        {
            SemiMajorAxis = a;
            Eccentricity = e;
            Mass = mass;
            DustMass = 0;
            GasMass = 0.0;
        }

        
    }
}
