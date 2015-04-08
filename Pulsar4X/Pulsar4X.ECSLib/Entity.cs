using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public partial class EntityManager
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

        [JsonObject(MemberSerialization.OptIn)]
        public class Entity
        {
            [JsonProperty("entityGuid")] 
            private readonly Guid _entityGuid;
            private int _entityID;
            private EntityManager _currentManager;
            private ComparableBitArray _dataBlobMask;

            public event EventHandler Deleting;
            public event EventHandler Deleted;

            public event EntityManagerChangeEvent ChangingManagers;
            public event EntityManagerChangeEvent ChangedManagers;

            [JsonConstructor]
            public Entity(Guid entityGuid)
            {
                _entityGuid = entityGuid;
                if (!EntityManager.FindEntityByGuid(entityGuid, out _currentManager, out _entityID))
                {
                    throw new GuidNotFoundException();
                }
                _dataBlobMask = _currentManager._entityMasks[_entityID];
            }

            public Entity(Guid entityGuid, int entityID, EntityManager currentManager)
            {
                _entityGuid = entityGuid;
                _entityID = entityID;
                _currentManager = currentManager;
            }

            public List<BaseDataBlob> GetAllDataBlobs()
            {
                return _currentManager.GetAllDataBlobsOfEntity(_entityID);
            }

            public T GetDataBlob<T>() where T : BaseDataBlob
            {
                return _currentManager.GetDataBlob<T>(_entityID);
            }

            public T GetDataBlob<T>(int typeIndex) where T : BaseDataBlob
            {
                return _currentManager.GetDataBlob<T>(_entityID, typeIndex);
            }

            public void SetDataBlob<T>(T dataBlob) where T : BaseDataBlob
            {
                _currentManager.SetDataBlob(_entityID, dataBlob);
            }

            public void DeleteEntity()
            {
                if (Deleting != null)
                {
                    Deleting(this, EventArgs.Empty);
                }

                _currentManager.RemoveEntity(_entityID);

                if (Deleted != null)
                {
                    Deleted(this, EventArgs.Empty);
                }
            }

            public void TransferEntity(EntityManager newManager)
            {
                if (ChangingManagers != null)
                {
                    ChangingManagers(this, new EntityManagerChangeEventArgs(_currentManager, newManager));
                }

                _entityID = _currentManager.TransferEntity(_entityID, newManager);
                _currentManager = newManager;

                if (ChangedManagers != null)
                {
                    ChangedManagers(this, new EntityManagerChangeEventArgs(_currentManager, newManager));
                }
            }
        }
    }
}
