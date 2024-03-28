using System.Collections.Generic;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Tests
{
    internal class TestHelper
    {
        protected Game _game;
        protected EntityManager _entityManager;
        protected Dictionary<string, GasBlueprint> _gasDictionary;
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

            var result = Entity.Create();
            _entityManager.AddEntity(result, new List<BaseDataBlob> { planetBodyDB, planetNameDB, atmosphere });

            return result;
        }
    }
}