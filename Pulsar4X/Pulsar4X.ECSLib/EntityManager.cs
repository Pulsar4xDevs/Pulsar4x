using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Pulsar4x.ECSLib.DataBlobs;

namespace Pulsar4x.ECSLib
{
    public class EntityManager
    {
        private List<int> m_entities;
        private Dictionary<Type, List<IDataBlob>> m_dataBlobMap;

        public EntityManager()
        {
            Clear();
        }

        /// <summary>
        /// Returns a COPY of the datablob of the specific entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool TryGetDataBlob<T>(int entity, out T dataBlob) where T : IDataBlob
        {
            if (m_dataBlobMap[typeof(T)][entity] == null)
            {
                dataBlob = default(T);
                return false;
            }
            else
            {
                dataBlob = (T)m_dataBlobMap[typeof(T)][entity];
                return true;
            }
        }

        /// <summary>
        /// Sets the DataBlob for the specified entity.
        /// Do not use to remove the datablob.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="dataBlob"></param>
        public void SetDataBlob<T>(int entity, T dataBlob) where T : IDataBlob
        {
            if (dataBlob == null)
            {
                throw new ArgumentNullException("dataBlob", "Cannot set datablob to null. Use RemoveDataBlob to remove.");
            }

            m_dataBlobMap[typeof(T)][entity] = dataBlob;
        }

        /// <summary>
        /// Removes the specified DataBlob from the entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public void RemoveDataBlob<T>(int entity) where T: IDataBlob
        {
            m_dataBlobMap[typeof(T)][entity] = null;
        }

        /// <summary>
        /// Returns a list of all DataBlobs with type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetAllDataBlobsOfType<T>() where T: IDataBlob
        {
            return m_dataBlobMap[typeof(T)].ConvertAll<T>(
                v => 
                    {
                        if (v == null)
                        {
                            return default(T);
                        }
                        return (T)v;
                    }
                );
        }

        public List<IDataBlob> GetAllDataBlobsOfEntity(int entity)
        {
            List<IDataBlob> entityDBs = new List<IDataBlob>();

            foreach (List<IDataBlob> entityDBMap in m_dataBlobMap.Values)
            {
                IDataBlob currDataBlob = entityDBMap[entity];
                if (currDataBlob != null)
                {
                    entityDBs.Add(currDataBlob);
                }
            }

            return entityDBs;
        }

        /// <summary>
        /// Returns a list of entity id's for entities that have datablob type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<int> GetAllEntitiesWithDataBlob<T>()
        {
            List<int> entitiesWithDBType = new List<int>();

            for (int i = 0; i < m_entities.Count; i++)
            {
                if (m_dataBlobMap[typeof(T)][i] != null)
                {
                    entitiesWithDBType.Add(i);
                }
            }

            return entitiesWithDBType;
        }

        /// <summary>
        /// Creates an entity with an entity slot.
        /// </summary>
        /// <returns>Entity ID of the new entity.</returns>
        public int CreateEntity()
        {
            int i;
            for (i = 0; i < m_entities.Count; i++)
            {
                if (i != m_entities[i])
                {
                    // Space open.
                    break;
                }
            }

            // Mark space claimed by making the index match the value.
            // Entities[7] == 7; on claimed spot.
            // Entities[7] == -1; on unclaimed spot.
            if (i == m_entities.Count)
            {
                m_entities.Add(i);
                // Make sure the entityDBMaps have enough space for this entity.
                foreach(List<IDataBlob> entityDBMap in m_dataBlobMap.Values)
                {
                    entityDBMap.Add(null);
                }
            }
            else
            {
                m_entities[i] = i;
                // Make sure the EntityDBMaps are null for this entity.
                // This should be done by RemoveEntity, but let's just be safe.
                foreach (List<IDataBlob> entityDBMap in m_dataBlobMap.Values)
                {
                    entityDBMap[i] = null;
                }
            }

            return i;

        }

        /// <summary>
        /// Adds an entity with the pre-existing datablobs to this EntityManager.
        /// </summary>
        /// <param name="dataBlobs"></param>
        public void AddEntity(List<IDataBlob> dataBlobs)
        {
            int entity = CreateEntity();
            foreach (IDataBlob dataBlob in dataBlobs)
            {
                m_dataBlobMap[dataBlob.GetType()][entity] = dataBlob;
            }
        }

        /// <summary>
        /// Removes this entity from this entity manager.
        /// </summary>
        /// <param name="entity"></param>
        public void RemoveEntity(int entity)
        {
            foreach (List<IDataBlob> entityDBMap in m_dataBlobMap.Values)
            {
                entityDBMap[entity] = null;
            }

            m_entities[entity] = -1;
        }

        /// <summary>
        /// Completely clears all entities and reinitializes the DataBlob maps.
        /// </summary>
        public void Clear()
        {
            m_entities = new List<int>();
            m_dataBlobMap = new Dictionary<Type, List<IDataBlob>>();

            // Use reflection to setup all our dataBlobMap.
            Type dataBlobInterface = typeof(IDataBlob);

            // Find all types that implement IDataBlob
            List<Type> dataBlobTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t =>
                    t != dataBlobInterface &&
                    t.GetInterfaces().ToList().Contains(dataBlobInterface)
                ).ToList();

            // Create a list in our dataBlobMap for each discovered type.
            foreach (Type dataBlobType in dataBlobTypes)
            {
                m_dataBlobMap[dataBlobType] = new List<IDataBlob>();
            }
        }
    }
}
