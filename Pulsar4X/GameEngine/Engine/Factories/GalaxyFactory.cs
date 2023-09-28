using System;
using System.Collections.Generic;
using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Engine
{
    public class GalaxyFactory
    {
        internal readonly Random SeedRNG;

        public SystemGenSettingsBlueprint Settings;

        public Dictionary<int, string> SystemIndexes;

        internal readonly StarSystemFactory StarSystemFactory;

        public GalaxyFactory(SystemGenSettingsBlueprint settings, int rngSeed = -1)
        {
            Settings = settings;
            if (rngSeed != -1)
            {
                SeedRNG = new Random(rngSeed);
            }
            else
            {
                SeedRNG = new Random();
            }

            StarSystemFactory = new StarSystemFactory(this);
        }

        private GalaxyFactory()
        { }
    }
}