using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using Pulsar4X.Blueprints;
using Pulsar4X.Engine;

namespace Pulsar4X.Galaxy
{
    public class GalaxyFactory
    {

        public SystemGenSettingsBlueprint Settings;

        public Dictionary<int, string> SystemIndexes;

        internal readonly StarSystemFactory StarSystemFactory;

        public GalaxyFactory(SystemGenSettingsBlueprint settings)
        {
            Settings = settings;
            StarSystemFactory = new StarSystemFactory(this);
        }

        private GalaxyFactory()
        { }

        public StarSystem GenerateSystem(Game game, string name, int seed)
        {
            return StarSystemFactory.CreateSystem(game, name, seed);
        }
    }
}