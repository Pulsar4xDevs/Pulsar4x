using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Pulsar4X.Blueprints;
using Pulsar4X.Datablobs;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Galaxy;
using Pulsar4X.Names;

namespace Pulsar4X.People
{
    public static class SpeciesFactory
    {
        public static Entity CreateFromBlueprint(StarSystem system, SpeciesBlueprint speciesBlueprint)
        {
            var species = Entity.Create();

            system.AddEntity(species, new List<BaseDataBlob>() {
                new NameDB(speciesBlueprint.Name),
                new SpeciesDB()
                {
                    BaseGravity = speciesBlueprint.Gravity.Ideal ?? 0,
                    MinimumGravityConstraint = speciesBlueprint.Gravity.Minimum ?? 0,
                    MaximumGravityConstraint = speciesBlueprint.Gravity.Maximum ?? 0,
                    BasePressure = speciesBlueprint.Pressure.Ideal ?? 0,
                    MinimumPressureConstraint = speciesBlueprint.Pressure.Minimum ?? 0,
                    MaximumPressureConstraint = speciesBlueprint.Pressure.Maximum ?? 0,
                    BaseTemperature = speciesBlueprint.Temperature.Ideal ?? 0,
                    MinimumTemperatureConstraint = speciesBlueprint.Temperature.Minimum ?? 0,
                    MaximumTemperatureConstraint = speciesBlueprint.Temperature.Maximum ?? 0,
                    BreathableGasSymbol = speciesBlueprint.BreathableGasSymbol ?? "O2",
                }
            });

            return species;
        }

        public static Entity CreateFromJson(Entity faction, EntityManager system, string filePath)
        {
            string fileContents = File.ReadAllText(filePath);
            var rootJson = JObject.Parse(fileContents);

            var name = rootJson["name"].ToString();
            var species = Entity.Create();

            system.AddEntity(species, new List<BaseDataBlob>() {
                new NameDB(name),
                new SpeciesDB()
                {
                    BaseGravity = (double?)rootJson["gravity"]["ideal"] ?? 0,
                    MinimumGravityConstraint = (double?)rootJson["gravity"]["minimum"] ?? 0,
                    MaximumGravityConstraint = (double?)rootJson["gravity"]["maximum"] ?? 0,
                    BasePressure = (double?)rootJson["pressure"]["ideal"] ?? 0,
                    MinimumPressureConstraint = (double?)rootJson["pressure"]["minimum"] ?? 0,
                    MaximumPressureConstraint = (double?)rootJson["pressure"]["maximum"] ?? 0,
                    BaseTemperature = (double?)rootJson["temperature"]["ideal"] ?? 0,
                    MinimumTemperatureConstraint = (double?)rootJson["temperature"]["minimum"] ?? 0,
                    MaximumTemperatureConstraint = (double?)rootJson["temperature"]["maximum"] ?? 0,
                    BreathableGasSymbol = (string?)rootJson["breathableGasSymbol"] ?? "O2",
                }
            });

            species.FactionOwnerID = faction.Id;
            faction.GetDataBlob<FactionInfoDB>().Species.Add(species);

            return species;
        }

        public static Entity CreateSpeciesHuman(Entity faction, EntityManager systemEntityManager)
        {
            NameDB name = new NameDB("Human");
            SpeciesDB speciesDB = CreateSpeciesDB_Human();
            var blobs = new List<BaseDataBlob> {name, speciesDB};
            Entity species = Entity.Create();
            species.FactionOwnerID = faction.Id;
            systemEntityManager.AddEntity(species, blobs);
            faction.GetDataBlob<FactionInfoDB>().Species.Add(species);
            return species;
        }

        private static SpeciesDB CreateSpeciesDB_Human()
        {
            double baseGravity = 1.0;
            double minimumGravityConstraint = 0.1;
            double maximumGravityConstraint = 1.9;
            double basePressure = 1.0;
            double minimumPressureConstraint = 0.4;
            double maximumPressureConstraint = 4.0;
            double baseTemperature = 14.0;
            double minimumTemperatureConstraint = -15.0;
            double maximumTemperatureConstraint = 45.0;
            SpeciesDB species = new SpeciesDB(baseGravity,
                minimumGravityConstraint, maximumGravityConstraint,
                basePressure, minimumPressureConstraint,
                maximumPressureConstraint, baseTemperature,
                minimumTemperatureConstraint, maximumTemperatureConstraint);

            return species;
        }

        public static Entity CreateSpeciesFromBlobs(Entity faction, EntityManager globalManager, NameDB nameDB, SpeciesDB speciesDB)
        {
            var blobs = new List<BaseDataBlob> { nameDB, speciesDB };
            Entity species = Entity.Create();
            species.FactionOwnerID = faction.Id;
            globalManager.AddEntity(species, blobs);
            faction.GetDataBlob<FactionInfoDB>().Species.Add(species);
            return species;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="faction"></param>
        /// <param name="systemEntityManager"></param>
        /// <param name="planetEntity"></param>
        /// <returns></returns>
        public static Entity CreateSpeciesForPlanet(Entity faction, EntityManager systemEntityManager, Entity planetEntity)
        {
            NameDB name = new NameDB("somename"); //where should we get the name from? maybe we should pass a string here.
            SpeciesDB speciesDB = CreateSpeciesDB_FromPlanet(planetEntity, systemEntityManager.RNG);
            var blobs = new List<BaseDataBlob> {name, speciesDB};
            Entity species = Entity.Create();
            species.FactionOwnerID = faction.Id;
            systemEntityManager.AddEntity(species, blobs);
            faction.GetDataBlob<FactionInfoDB>().Species.Add(species);
            return species;
        }

        private static SpeciesDB CreateSpeciesDB_FromPlanet(Entity planetEntity, Random rng)
        {

            MassVolumeDB masvolinfo = planetEntity.GetDataBlob<MassVolumeDB>();
            SystemBodyInfoDB sysbodyinfo = planetEntity.GetDataBlob<SystemBodyInfoDB>();
            AtmosphereDB atmoinfo = planetEntity.GetDataBlob<AtmosphereDB>();


            //throw new NotImplementedException();
            double baseGravity = masvolinfo.SurfaceGravity;
            double minimumGravityConstraint = 0.1;//rnd.Next(planetInfo.SurfaceGravity, 0.1);
            double maximumGravityConstraint = 1.9;
            double basePressure = atmoinfo.Pressure;
            double minimumPressureConstraint = 0.4;
            double maximumPressureConstraint = 4.0;
            double baseTemperature = sysbodyinfo.BaseTemperature;
            double minimumTemperatureConstraint = -15.0;
            double maximumTemperatureConstraint = 45.0;
            SpeciesDB species = new SpeciesDB(baseGravity,
                minimumGravityConstraint, maximumGravityConstraint,
                basePressure, minimumPressureConstraint,
                maximumPressureConstraint, baseTemperature,
                minimumTemperatureConstraint, maximumTemperatureConstraint);

            return species;
        }
    }
}
