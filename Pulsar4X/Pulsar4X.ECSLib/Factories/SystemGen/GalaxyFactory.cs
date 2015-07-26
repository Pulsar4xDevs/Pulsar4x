using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public enum SystemBand
    {
        InnerBand,
        HabitableBand,
        OuterBand,
    };

    public class GalaxyFactory
    {
        internal readonly Random SeedRNG;

        public SystemGenSettingsSD Settings;

        internal readonly StarSystemFactory StarSystemFactory;

        public GalaxyFactory(bool initToDefault = false, int rngSeed = -1)
        {
            if (initToDefault)
            {
                Settings = SystemGenSettingsSD.DefaultSettings;
            }
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