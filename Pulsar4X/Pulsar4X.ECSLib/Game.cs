using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib.DataBlobs;

namespace Pulsar4X.ECSLib
{
    public class Game
    {
        public EntityManager GlobalManager { get { return m_globalManager; } }
        private EntityManager m_globalManager;

        public static Game Instance { get { return m_instance; } }
        private static Game m_instance;

        public DateTime CurrentDateTime { get; set; }

        public Game()
        {
            m_globalManager = new EntityManager();

            m_instance = this;
        }

        public void EntityManagerTests()
        {
            // Create an entity with individual DataBlobs.
            int planet = GlobalManager.CreateEntity();
            GlobalManager.SetDataBlob<OrbitDB>(planet, OrbitDB.FromStationary(planet, 5));
            GlobalManager.SetDataBlob<PopulationDB>(planet, new PopulationDB(planet, 10));

            // Create an entity with a DataBlobList.
            List<IDataBlob> dataBlobs = new List<IDataBlob>();
            dataBlobs.Add(OrbitDB.FromStationary(-1, 2));
            GlobalManager.AddEntity(dataBlobs);

            // Create one more, just for kicks.
            dataBlobs.Add(new PopulationDB(-1, 9));
            GlobalManager.AddEntity(dataBlobs);

            // Get all DataBlobs of a specific type.
            List<PopulationDB> populations = GlobalManager.GetAllDataBlobsOfType<PopulationDB>();
            List<OrbitDB> orbits = GlobalManager.GetAllDataBlobsOfType<OrbitDB>();

            // Get all DataBlobs of a specific entity.
            dataBlobs = GlobalManager.GetAllDataBlobsOfEntity(planet);

            // Remove an entity.
            GlobalManager.RemoveEntity(planet);

            // Add a new entity (using a list of DataBlobs.
            GlobalManager.AddEntity(dataBlobs);

            // Find all entities with a specific DataBlob.
            List<int> populatedEntities = GlobalManager.GetAllEntitiesWithDataBlob<PopulationDB>();

            // Get the Population DB of a specific entity.
            PopulationDB planetPopDB = GlobalManager.GetDataBlob<PopulationDB>(planet);

            // Change the planet Pop.
            planetPopDB.PopulationSize += 5;

            // Get the current value.
            PopulationDB planetPopDB2 = GlobalManager.GetDataBlob<PopulationDB>(planet);

            if (planetPopDB.PopulationSize != planetPopDB2.PopulationSize)
            {
                // Note, we wont hit this because the value DID change.
                throw new InvalidOperationException();
            }

            // Forget it, remove the DataBlob.
            GlobalManager.SetDataBlob<PopulationDB>(planet, null);

            if (GlobalManager.GetDataBlob<PopulationDB>(1) == null)
            {
                // Will hit this!
                // Entity 1 doesn't have a population, so GetDataBlob returns null.

                // This crap is so you can reliably breakpoint this (even in release mode) without it being optimized away.
                int i = 0;
                i++;
            }
            else
            {
                // Wont hit this!
                // Since there's no pop!
                throw new InvalidOperationException();
            }

            // Remove the crap we added.
            GlobalManager.RemoveEntity(planet);
            GlobalManager.RemoveEntity(planet + 1);
            GlobalManager.RemoveEntity(planet + 2);
        }
    }
}
