using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.DataBlobs;

namespace Pulsar4X.ECSLib.Factories
{
    public static class SystemBodyFactory
    {
        public static Entity CreateMainStar(EntityManager globalManager, StarSystem starSystem, string name)
        {
            var nameDB = new NameDB(); //Not sure how to name it properly
            var starInfoDB = new StarInfoDB();
            var massVolumeDB = new MassVolumeDB(1, 1);
            var orbitDB = new OrbitDB(); //stationary

            var blobs = new List<BaseDataBlob>()
            {
                nameDB, 
                starInfoDB,
                massVolumeDB,
                orbitDB
            };

            Entity starEntity = Entity.Create(globalManager, blobs);

            return starEntity;
        }

        public static Entity CreateSubStar(EntityManager starSystemManager, Entity parentStar, string name)
        {
            MassVolumeDB parentMassVolumeDB = parentStar.GetDataBlob<MassVolumeDB>();

            var nameDB = new NameDB();
            var starInfoDB = new StarInfoDB();
            var massVolumeDB = new MassVolumeDB(1, 1);
            var orbitDB = OrbitDB.FromMajorPlanetFormat(parentStar, parentMassVolumeDB, massVolumeDB, 0.5, 1, 0, 0, 0, 0, new DateTime());

            var blobs = new List<BaseDataBlob>()
            {
                nameDB,
                starInfoDB,
                massVolumeDB,
                orbitDB
            };

            Entity starEntity = Entity.Create(starSystemManager, blobs);

            return starEntity;
        }

        public static Entity CreatePlanet(EntityManager starSystemManager, Entity parentBody, string name)
        {
            MassVolumeDB parentMassVolumeDB = parentBody.GetDataBlob<MassVolumeDB>();

            var nameDB = new NameDB();
            var systemBodyDB = new SystemBodyDB();
            var massVolumeDB = new MassVolumeDB(1, 1);
            var orbitDB = OrbitDB.FromMajorPlanetFormat(parentBody, parentMassVolumeDB, massVolumeDB, 1, 1, 0, 0, 0, 0, new DateTime());

            var blobs = new List<BaseDataBlob>()
            {
                nameDB,
                systemBodyDB,
                massVolumeDB,
                orbitDB
            };

            Entity planetEntity = Entity.Create(starSystemManager, blobs);

            return planetEntity;
        }
    }
}
