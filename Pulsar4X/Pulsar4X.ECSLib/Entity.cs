using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Pulsar4X.ECSLib
{

    public delegate void EntityManagerChangeEvent(object sender, EntityManagerChangeEventArgs args);

    public class EntityManagerChangeEventArgs
    {
        public readonly EntityManager OldManager;
        public readonly EntityManager NewManager;

        public EntityManagerChangeEventArgs(EntityManager oldManager, EntityManager newManager)
        {
            OldManager = oldManager;
            NewManager = newManager;
        }
    }

    [JsonConverter(typeof(EntityConverter))]
    public class Entity
    {
        public Guid Guid { get; private set; }
        public int ID { get; private set; }
        public EntityManager Manager { get; private set; }
        public ComparableBitArray DataBlobMask { get { return Manager.GetMask(this); } }

        public event EventHandler Deleting;
        public event EventHandler Deleted;

        public event EntityManagerChangeEvent ChangingManagers;
        public event EntityManagerChangeEvent ChangedManagers;

        private static EntityManager _invalidManager;
        public static Entity InvalidEntity
        {
            get
            {
                lock (lockObj)
                {
                    if (_invalidEntity == null)
                        _invalidEntity = new Entity(Guid.Empty, _invalidManager);
                    return _invalidEntity;
                }
            }
        }
        private static Entity _invalidEntity;

        private static object lockObj = new object();


        internal Entity(Guid guid, EntityManager currentManager)
        {
            Guid = guid;
            Manager = currentManager;

            if (_invalidManager == null)
                _invalidManager = new EntityManager();

        }

        public bool IsValid { get { return Manager.IsValidEntity(this); } }

        public List<BaseDataBlob> GetAllDataBlobs()
        {
            return Manager.GetAllDataBlobsOfEntity(this);
        }

        public T GetDataBlob<T>() where T : BaseDataBlob
        {
            return Manager.GetDataBlob<T>(ID);
        }

        public T GetDataBlob<T>(int typeIndex) where T : BaseDataBlob
        {
            return Manager.GetDataBlob<T>(ID, typeIndex);
        }

        public void SetDataBlob<T>(T dataBlob) where T : BaseDataBlob
        {
            Manager.SetDataBlob(ID, dataBlob);
        }

        public void SetDataBlob(BaseDataBlob dataBlob, int typeIndex)
        {
            Manager.SetDataBlob(ID, dataBlob, typeIndex);
        }

        public void RemoveDataBlob<T>() where T : BaseDataBlob
        {
            Manager.RemoveDataBlob<T>(ID);
        }

        public void RemoveDataBlob(int typeIndex)
        {
            Manager.RemoveDataBlob(ID, typeIndex);
        }

        public void Delete()
        {
            if (Deleting != null)
            {
                Deleting(this, EventArgs.Empty);
            }

            Manager.RemoveEntity(this);

            if (Deleted != null)
            {
                Deleted(this, EventArgs.Empty);
            }
        }

        public void Transfer(EntityManager newManager)
        {
            if (newManager == null)
            {
                throw new ArgumentNullException("newManager");
            }

            if (ChangingManagers != null)
            {
                ChangingManagers(this, new EntityManagerChangeEventArgs(Manager, newManager));
            }

            Manager.TransferEntity(this, newManager);
            Manager = newManager;

            if (ChangedManagers != null)
            {
                ChangedManagers(this, new EntityManagerChangeEventArgs(Manager, newManager));
            }
        }

        /// <summary>
        /// Clones this entity into the specified entity manager, returns the new clone.
        /// </summary>
        /// <param name="toManager">Manager you want to place the new clone into.</param>
        /// <returns>The new clone.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public Entity Clone(EntityManager toManager)
        {
            List<BaseDataBlob> dataBlobs = GetAllDataBlobs();
            List<BaseDataBlob> clonedDataBlobs = new List<BaseDataBlob>(dataBlobs.Count);

            foreach (BaseDataBlob baseDataBlob in dataBlobs)
            {
                clonedDataBlobs.Add((BaseDataBlob) baseDataBlob.Clone());
            } 

            return Create(toManager, clonedDataBlobs);
        }

        internal void SetID(int newID)
        {
            ID = newID;
        }

        public static Entity Create(EntityManager manager, List<BaseDataBlob> dataBlobs = null)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            return manager.CreateEntity(dataBlobs);
        }

#if DEBUG
        public override string ToString()
        {
            return Guid.ToString();
        }
#endif

        public bool HasDataBlob<T>() where T : BaseDataBlob
        {
            int typeIndex = EntityManager.GetTypeIndex<T>();
            return DataBlobMask[typeIndex];
        }
    }

    public class EntityConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Guid));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Entity entity;
            Guid entityGuid = Guid.Parse(reader.Value.ToString());
            if (entityGuid == Guid.Empty)
                return Entity.InvalidEntity;
            if (EntityManager.FindEntityByGuid(entityGuid, out entity))
                return entity;

            // If we couldn't find the Guid (Entity is in a manager that hasn't loaded)
            // create the entity in the Global Manager. We'll transfer it to the correct manager
            // when we deserialize it.
            return new Entity(entityGuid, Game.Instance.GlobalManager);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((Entity)value).Guid);
        }
    }
}
