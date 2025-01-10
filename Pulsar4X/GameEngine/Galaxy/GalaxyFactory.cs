using System;
using System.Collections.Generic;
using Pulsar4X.Blueprints;

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
    }
}