using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Pulsar4X.ECSLib.DataBlobs;
using Pulsar4X.Helpers;

namespace Pulsar4X.ECSLib
{
    public class EntityManager
    {
        private List<int> m_entities;
        private List<ComparableBitArray> m_entityMasks;

        private Dictionary<Type, int> m_dataBlobTypes;
        private List<List<BaseDataBlob>> m_dataBlobMap;

        public EntityManager()
        {
            Clear();
        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Slower than GetDataBlob(entity, typeIndex)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public T GetDataBlob<T>(int entity) where T : BaseDataBlob
        {
            int typeIndex = GetDataBlobTypeIndex<T>();
            return GetDataBlob<T>(entity, typeIndex);
        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Fastest direct lookup available.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="typeIndex"></param>
        /// <returns></returns>
        public T GetDataBlob<T>(int entity, int typeIndex) where T : BaseDataBlob
        {
            return (T)m_dataBlobMap[typeIndex][entity];
        }

        /// <summary>
        /// Sets the DataBlob for the specified entity.
        /// Slower than SetDataBlob(entity, dataBlob, typeIndex)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="dataBlob"></param>
        public void SetDataBlob<T>(int entity, T dataBlob) where T : BaseDataBlob
        {
            int typeIndex = GetDataBlobTypeIndex<T>();
            SetDataBlob(entity, dataBlob, typeIndex);
        }

        /// <summary>
        /// Sets the DataBlob for the specified entity.
        /// Fastest DataBlob setting available.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="dataBlob"></param>
        /// <param name="typeIndex"></param>
        public void SetDataBlob(int entity, BaseDataBlob dataBlob, int typeIndex)
        {
            if (dataBlob == null)
            {
                throw new ArgumentNullException("dataBlob", "Do not use SetDataBlob to remove a datablob. Use RemoveDataBlob.");
            }

            dataBlob.Entity = entity;
            m_dataBlobMap[typeIndex][entity] = dataBlob;
            m_entityMasks[entity][typeIndex] = true;
        }

        /// <summary>
        /// Removes the DataBlob from the specified entity.
        /// Slower than RemoveDataBlob(entity, typeIndex).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        public void RemoveDataBlob<T>(int entity) where T : BaseDataBlob
        {
            int typeIndex = GetDataBlobTypeIndex<T>();
            RemoveDataBlob(entity, typeIndex);
        }

        /// <summary>
        /// Removes the DataBlob from the specified entity.
        /// Slower than RemoveDataBlob(entity, typeIndex).
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="T"></param>
        public void RemoveDataBlob(int entity, Type T)
        {
            int typeIndex;
            if (TryGetDataBlobTypeIndex(T, out typeIndex))
            {
                RemoveDataBlob(entity, typeIndex);
            }
            else
            {
                throw new ArgumentException("Type not found in typeMap.");
            }
        }

        /// <summary>
        /// Removes the DataBlob from the specified entity.
        /// Fastest DataBlob removal available.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="typeIndex"></param>
        public void RemoveDataBlob(int entity, int typeIndex)
        {
            m_dataBlobMap[typeIndex][entity] = null;
            m_entityMasks[entity][typeIndex] = false;
        }

        /// <summary>
        /// Returns a list of all DataBlobs with type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> GetAllDataBlobsOfType<T>() where T: BaseDataBlob
        {
            return m_dataBlobMap[GetDataBlobTypeIndex<T>()].ConvertAll<T>(v => (T)v);
        }

        /// <summary>
        /// Returns a list of all DataBlobs for a given entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public List<BaseDataBlob> GetAllDataBlobsOfEntity(int entity)
        {
            List<BaseDataBlob> entityDBs = new List<BaseDataBlob>();
            ComparableBitArray entityMask = m_entityMasks[entity];

            for (int typeIndex = 0; typeIndex < m_dataBlobTypes.Count; typeIndex++)
            {
                if (entityMask[typeIndex])
                {
                    entityDBs.Add(GetDataBlob<BaseDataBlob>(entity, typeIndex));
                }
            }

            return entityDBs;
        }

        /// <summary>
        /// Returns a list of entity id's for entities that have datablob type T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<int> GetAllEntitiesWithDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = GetDataBlobTypeIndex<T>();

            ComparableBitArray dataBlobMask = BlankDataBlobMask();
            dataBlobMask[typeIndex] = true;

            return GetAllEntitiesWithDataBlobs(dataBlobMask);
        }

        /// <summary>
        /// Returns a list of entity id's for entities that contain all dataBlobs defined by
        /// the dataBlobMask.
        /// </summary>
        /// <param name="dataBlobMask"></param>
        /// <returns></returns>
        public List<int> GetAllEntitiesWithDataBlobs(ComparableBitArray dataBlobMask)
        {
            if (dataBlobMask.Length != m_dataBlobTypes.Count)
            {
                throw new ArgumentException("dataBlobMask must contain a bit value for each dataBlobType.");
            }

            List<int> entities = new List<int>();

            for (int entity = 0; entity < m_entityMasks.Count; entity++)
            {
                if ((m_entityMasks[entity] & dataBlobMask) == dataBlobMask)
                {
                    entities.Add(entity);
                }
            }

            return entities;
        }

        public int GetFirstEntityWithDataBlob<T>() where T : BaseDataBlob
        {
            return GetFirstEntityWithDataBlob(GetDataBlobTypeIndex<T>());
        }

        public int GetFirstEntityWithDataBlob(int typeIndex)
        {
            List<BaseDataBlob> dataBlobType = m_dataBlobMap[typeIndex];
            for (int i = 0; i < m_entities.Count; i++)
            {
                if (dataBlobType[i] != null)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns a blank DataBlob mask with the correct number of entries.
        /// </summary>
        /// <returns></returns>
        public ComparableBitArray BlankDataBlobMask()
        {
            return new ComparableBitArray(m_dataBlobTypes.Count);
        }

        /// <summary>
        /// Creates an entity with an entity slot.
        /// </summary>
        /// <returns>Entity ID of the new entity.</returns>
        public int CreateEntity()
        {
            int entityID;
            for (entityID = 0; entityID < m_entities.Count; entityID++)
            {
                if (entityID != m_entities[entityID])
                {
                    // Space open.
                    break;
                }
            }

            // Mark space claimed by making the index match the value.
            // Entities[7] == 7; on claimed spot.
            // Entities[7] == -1; on unclaimed spot.
            if (entityID == m_entities.Count)
            {
                m_entities.Add(entityID);
                m_entityMasks.Add(new ComparableBitArray(m_dataBlobTypes.Count));
                // Make sure the entityDBMaps have enough space for this entity.
                foreach(List<BaseDataBlob> entityDBMap in m_dataBlobMap)
                {
                    entityDBMap.Add(null);
                }
            }
            else
            {
                m_entities[entityID] = entityID;
                // Make sure the EntityDBMaps are null for this entity.
                // This should be done by RemoveEntity, but let's just be safe.
                for (int typeIndex = 0; typeIndex < m_dataBlobTypes.Count; typeIndex++)
                {
                    m_dataBlobMap[typeIndex][entityID] = null;
                }

                m_entityMasks[entityID] = new ComparableBitArray(m_dataBlobTypes.Count);
            }

            return entityID;
        }

        /// <summary>
        /// Adds an entity with the pre-existing datablobs to this EntityManager.
        /// </summary>
        /// <param name="dataBlobs"></param>
        public int CreateEntity(List<BaseDataBlob> dataBlobs)
        {
            int entity = CreateEntity();

            foreach (BaseDataBlob dataBlob in dataBlobs)
            {
                int typeIndex;
                TryGetDataBlobTypeIndex(dataBlob.GetType(), out typeIndex);
                SetDataBlob(entity, dataBlob, typeIndex);
            }

            return entity;
        }

        /// <summary>
        /// Removes this entity from this entity manager.
        /// </summary>
        /// <param name="entity"></param>
        public void RemoveEntity(int entity)
        {
            foreach (List<BaseDataBlob> dataBlobType in m_dataBlobMap)
            {
                dataBlobType[entity] = null;
            }
            m_entityMasks[entity] = new ComparableBitArray(m_dataBlobTypes.Count);
            m_entities[entity] = -1;
        }

        /// <summary>
        /// Completely clears all entities and reinitializes the DataBlob maps.
        /// </summary>
        public void Clear()
        {
            m_entities = new List<int>();
            m_entityMasks = new List<ComparableBitArray>();

            m_dataBlobTypes = new Dictionary<Type, int>();
            m_dataBlobMap = new List<List<BaseDataBlob>>();

            // Use reflection to setup all our dataBlobMap.
            // Find all types that implement BaseDataBlob
            List<Type> dataBlobTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t =>
                    t != typeof(BaseDataBlob) &&
                    t.IsSubclassOf(typeof(BaseDataBlob))
                ).ToList();

            // Create a list in our dataBlobMap for each discovered type.
            int i = 0;
            foreach (Type dataBlobType in dataBlobTypes)
            {
                m_dataBlobTypes.Add(dataBlobType, i);
                m_dataBlobMap.Add(new List<BaseDataBlob>());
                i++;
            }
        }

        /// <summary>
        /// Returns the TypeIndex of a specified dataBlobType.
        /// </summary>
        /// <param name="dataBlobType"></param>
        /// <param name="typeIndex"></param>
        /// <returns></returns>
        public bool TryGetDataBlobTypeIndex(Type dataBlobType, out int typeIndex)
        {
            return m_dataBlobTypes.TryGetValue(dataBlobType, out typeIndex);
        }

        /// <summary>
        /// Faster than TryGetDataBlobTypeIndex and uses generics for type safety.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public int GetDataBlobTypeIndex<T>() where T : BaseDataBlob
        {
            return m_dataBlobTypes[typeof(T)];
        }
    }
}
