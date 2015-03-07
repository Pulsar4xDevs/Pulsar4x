using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4x.ECSLib;
using Pulsar4x.ECSLib.DataBlobs;

namespace Pulsar4X.ECSLib
{
    public class Game
    {
        EntityManager m_globalManager;
        EntityManager GlobalManager { get { return m_globalManager; } }

        public Game()
        {
            m_globalManager = new EntityManager();
        }

        public void EntityManagerTests()
        {
            // Create an entity with individual DataBlobs.
            int planet = GlobalManager.CreateEntity();
            GlobalManager.SetDataBlob<OrbitDB>(planet, new OrbitDB(5));
            GlobalManager.SetDataBlob<PopulationDB>(planet, new PopulationDB(10));

            // Create an entity with a DataBlobList.
            List<IDataBlob> dataBlobs = new List<IDataBlob>();
            dataBlobs.Add(new OrbitDB(2));
            GlobalManager.AddEntity(dataBlobs);

            // Create one more, just for kicks.
            dataBlobs.Add(new PopulationDB(9));
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
            PopulationDB planetPopDB;
            GlobalManager.TryGetDataBlob<PopulationDB>(planet, out planetPopDB);

            // Change the planet Pop.
            planetPopDB = new PopulationDB(planetPopDB.PopulationSize + 5);

            // Get the current value.
            PopulationDB planetPopDB2;
            GlobalManager.TryGetDataBlob<PopulationDB>(planet, out planetPopDB2);

            if (planetPopDB.PopulationSize == planetPopDB2.PopulationSize)
            {
                // Note, we wont hit this because the value didn't actually change.
                throw new InvalidOperationException();
            }

            // Update the entity with new changes.
            GlobalManager.SetDataBlob<PopulationDB>(planet, planetPopDB);

            // Forget it, remove the DataBlob.
            GlobalManager.RemoveDataBlob<PopulationDB>(planet);

            if (GlobalManager.TryGetDataBlob<PopulationDB>(1, out planetPopDB))
            {
                // Wont hit this!
                // Entity 1 doesn't have a population, so TryGetDataBlob fails.
                throw new InvalidOperationException();
            }
            else
            {
                // Will hit this!
                // planetPopDB is default(PopulationDB);

                // This crap is so you can reliably breakpoint this (even in release mode) without it being optimized away.
                int i = 0;
                i++;
            }

            // Remove the crap we added.
            GlobalManager.RemoveEntity(planet);
            GlobalManager.RemoveEntity(planet + 1);
            GlobalManager.RemoveEntity(planet + 2);
        }
    }
}
