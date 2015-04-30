using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulsar4X.ECSLib.Factories
{
    public static class SpeciesFactory
    {
        public static Entity CreateSpeciesHuman(Entity faction, EntityManager systemEntityManager)
        {
            NameDB name = new NameDB(faction, "Human");
            SpeciesDB speciesDB = CreateSpeciesDB_Human();
            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            blobs.Add(name);
            blobs.Add(speciesDB);
            Entity species = Entity.Create(systemEntityManager, blobs);

            return species;
        }

        private static SpeciesDB CreateSpeciesDB_Human()
        {
            double BaseGravity = 1.0;
            double MinimumGravityConstraint = 0.1;
            double MaximumGravityConstraint = 1.9;
            double BasePressure = 1.0;
            double MinimumPressureConstraint = 0.4;
            double MaximumPressureConstraint = 4.0;
            double BaseTemperature = 14.0;
            double MinimumTemperatureConstraint = -15.0;
            double MaximumTemperatureConstraint = 45.0;
            SpeciesDB species = new SpeciesDB(BaseGravity, 
                MinimumGravityConstraint, MaximumGravityConstraint, 
                BasePressure, MinimumPressureConstraint,
                MaximumPressureConstraint, BaseTemperature, 
                MinimumTemperatureConstraint, MaximumTemperatureConstraint);

            return species;
        }


        public static Entity CreateSpeciesForPlanet(Entity faction, EntityManager systemEntityManager, Entity planetEntity)
        {
            NameDB name = new NameDB(faction, "somename"); //where should we get the name from? maybe we shoudl pass a string here.
            SpeciesDB speciesDB = CreateSpeciesDB_FromPlanet(planetEntity);
            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            blobs.Add(name);
            blobs.Add(speciesDB);
            Entity species = Entity.Create(systemEntityManager, blobs);

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
            double BaseGravity = masvolinfo.SurfaceGravity;
            double MinimumGravityConstraint = 0.1;//rnd.Next(planetInfo.SurfaceGravity, 0.1);
            double MaximumGravityConstraint = 1.9;
            double BasePressure = atmoinfo.Pressure;
            double MinimumPressureConstraint = 0.4;
            double MaximumPressureConstraint = 4.0;
            double BaseTemperature = sysbodyinfo.BaseTemperature;
            double MinimumTemperatureConstraint = -15.0;
            double MaximumTemperatureConstraint = 45.0;
            SpeciesDB species = new SpeciesDB(BaseGravity,
                MinimumGravityConstraint, MaximumGravityConstraint,
                BasePressure, MinimumPressureConstraint,
                MaximumPressureConstraint, BaseTemperature,
                MinimumTemperatureConstraint, MaximumTemperatureConstraint);

            return species;
        }
    }
}
