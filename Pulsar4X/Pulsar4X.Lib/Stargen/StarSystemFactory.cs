using System;
using Pulsar4X.Entities;
using log4net;


namespace Pulsar4X.Stargen
{
    public class StarSystemFactory
    {
        private const double MinAge = 1.0E9;
        private const double MaxAge = 6.0E9;

        public static readonly ILog logger = LogManager.GetLogger(typeof(StarSystemFactory));

        private readonly double _minimumStellarAge;
        private readonly double _maximumStellarAge;
        private readonly bool _generateMoons;

        public StarSystemFactory(bool genMoons) : this(MinAge, MaxAge, genMoons)
        {
        }

        public StarSystemFactory(double minStellarAge, double maxStellarAge,  bool genMoons)
        {
            _minimumStellarAge = minStellarAge;
            _maximumStellarAge = maxStellarAge;
            _generateMoons = genMoons;
        }
        
        public StarSystem Create(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty.");

            var accrete = new Accrete(_minimumStellarAge, _maximumStellarAge,  _generateMoons);
            var ss = accrete.Create(name);

            return ss;
        }
    }
}
