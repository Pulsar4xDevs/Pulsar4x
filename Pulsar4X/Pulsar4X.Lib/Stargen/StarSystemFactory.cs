using Pulsar4X.Entities;

namespace Pulsar4X.Stargen
{
    public class StarSystemFactory
    {
        private readonly double _minimumStellarAge;
        private readonly double _maximumStellarAge;
        private readonly bool _generateMoons;


        public StarSystemFactory(double minStellarAge, double maxStellarAge,  bool genMoons)
        {
            _minimumStellarAge = minStellarAge;
            _maximumStellarAge = maxStellarAge;
            _generateMoons = genMoons;
        }
        
        public StarSystem Create(string name)
        {
            var accrete = new Accrete(_minimumStellarAge, _maximumStellarAge,  _generateMoons);
            var ss = accrete.Create(name);

            return ss;
        }
    }
}
