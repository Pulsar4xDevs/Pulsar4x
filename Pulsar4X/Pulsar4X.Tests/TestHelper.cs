using System.Collections.Generic;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    internal class TestHelper
    {
        protected Game _game;
        protected EntityManager _entityManager;
        protected Dictionary<string, AtmosphericGasSD> _gasDictionary;
        protected SpeciesDB _humans;
        protected AtmosphereDB _atmosphere;

        protected Entity GetPlanet(float baseTemperature, float albedo, double gravity, AtmosphereDB atmosphere = null)
        {
            SystemBodyInfoDB planetBodyDB = new() {
                BodyType = BodyType.Terrestrial,
                SupportsPopulations = true,
                Gravity = gravity,
                BaseTemperature = baseTemperature,
                Albedo = new PercentValue(albedo)
            };
            NameDB planetNameDB = new("Test Planet");

            return new(_entityManager, new List<BaseDataBlob> { planetBodyDB, planetNameDB, atmosphere });
        }
    }
}