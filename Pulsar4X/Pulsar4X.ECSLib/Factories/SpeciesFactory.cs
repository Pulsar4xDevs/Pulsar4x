using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class SpeciesFactory
    {
        public static Entity CreateSpeciesHuman(Entity faction, EntityManager systemEntityManager)
        {
            NameDB name = new NameDB("Human");
            SpeciesDB speciesDB = CreateSpeciesDB_Human();
            var blobs = new List<BaseDataBlob> {name, speciesDB};
            Entity species = new Entity(systemEntityManager, blobs);
            faction.GetDataBlob<FactionDB>().Species.Add(species);
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
            Entity species = new Entity(systemEntityManager, blobs);
            faction.GetDataBlob<FactionDB>().Species.Add(species);
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
            SystemBodyDB sysbodyinfo = planetEntity.GetDataBlob<SystemBodyDB>();
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
