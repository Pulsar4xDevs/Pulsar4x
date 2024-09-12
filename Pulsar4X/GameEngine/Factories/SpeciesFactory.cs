using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Engine
{
    public static class SpeciesFactory
    {
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
                    MaximumGravityConstraint = (double?)rootJson["gravity"]["maxiumum"] ?? 0,
                    BasePressure = (double?)rootJson["pressure"]["ideal"] ?? 0,
                    MinimumPressureConstraint = (double?)rootJson["pressure"]["minimum"] ?? 0,
                    MaximumPressureConstraint = (double?)rootJson["pressure"]["maxiumum"] ?? 0,
                    BaseTemperature = (double?)rootJson["temperature"]["ideal"] ?? 0,
                    MinimumTemperatureConstraint = (double?)rootJson["temperature"]["minimum"] ?? 0,
                    MaximumTemperatureConstraint = (double?)rootJson["temperature"]["maxiumum"] ?? 0,
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
            SpeciesDB speciesDB = CreateSpeciesDB_FromPlanet(planetEntity);
            var blobs = new List<BaseDataBlob> {name, speciesDB};
            Entity species = Entity.Create();
            species.FactionOwnerID = faction.Id;
            systemEntityManager.AddEntity(species, blobs);
            faction.GetDataBlob<FactionInfoDB>().Species.Add(species);
            return species;
        }

        private static SpeciesDB CreateSpeciesDB_FromPlanet(Entity planetEntity, int? seed = null)
        {
            Random rnd;
            if (seed != null)
                rnd = new Random((int)seed);
            else
                rnd = new Random();

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
