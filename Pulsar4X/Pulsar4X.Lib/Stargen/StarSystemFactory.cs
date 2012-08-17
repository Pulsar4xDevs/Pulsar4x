using System;
using Pulsar4X.Entities;
using log4net;


namespace Pulsar4X.Stargen
{
    public class StarSystemFactory
    {

        public static readonly ILog logger = LogManager.GetLogger(typeof(StarSystemFactory));

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
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty.");

            var accrete = new Accrete(_minimumStellarAge, _maximumStellarAge,  _generateMoons);
            var ss = accrete.Create(name);

            return ss;
        }
    }
}
