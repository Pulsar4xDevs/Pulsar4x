using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public delegate void EntityChangeHandler (EntityChangeData.EntityChangeType changeType, BaseDataBlob db);

    [DebuggerDisplay("{" + nameof(DebugDisplay) + "}")]
    [JsonConverter(typeof(EntityConverter))]
    [PublicAPI]
    public sealed class Entity : ProtoEntity
    {
        public event EntityChangeHandler ChangeEvent;
        // Index slot of this entity's datablobs in its EntityManager.
        internal int ID;

        private string DebugDisplay => GetDataBlob<NameDB>()?.OwnersName ?? Guid.ToString();

        [NotNull]
        [JsonIgnore]
        public EntityManager Manager { get; private set; }
        [JsonProperty]
        public Guid FactionOwner { get; internal set; }

        public DateTime StarSysDateTime => Manager.StarSysDateTime;

        [NotNull]
        [PublicAPI]
        public new ReadOnlyCollection<BaseDataBlob> DataBlobs => IsValid ? new ReadOnlyCollection<BaseDataBlob>(Manager.GetAllDataBlobsForEntity(ID)) : new ReadOnlyCollection<BaseDataBlob>(new List<BaseDataBlob>());

        private static readonly EntityManager InvalidManager = EntityManager.InvalidManager;

        /// <summary>
        /// Static entity reference to an invalid entity.
        /// 
        /// Functions must never return a null entity. Instead, return InvalidEntity.
        /// </summary>
        [NotNull]
        [PublicAPI]
        public static readonly Entity InvalidEntity = new Entity();

        public void InvokeChangeEvent(EntityChangeData.EntityChangeType changeType, BaseDataBlob db)
        {
            ChangeEvent?.Invoke(changeType, db); 
        }

        #region Entity Constructors
        private Entity()
        {
            Manager = InvalidManager;
        }

        internal Entity([NotNull] EntityManager manager, IEnumerable<BaseDataBlob> dataBlobs = null) : this(Guid.NewGuid(), manager, Guid.Empty, dataBlobs) { }
        internal Entity([NotNull] EntityManager manager, Guid factionOwner, IEnumerable<BaseDataBlob> dataBlobs = null) : this(Guid.NewGuid(), manager,  factionOwner, dataBlobs) { }


        internal Entity(Guid id, [NotNull] EntityManager manager, Guid factionOwner,  IEnumerable<BaseDataBlob> dataBlobs = null)
        {
            Manager = manager;
            Guid = id;
            FactionOwner = factionOwner;
            //This is problematic, currently, if a datablob references it's own entity (ie namedb in faction entity) the entity will get a new guid. 
            //and (presumably) the db will point to an empty entity. 
            //TODO: should we throw an exception instead of just replacing the guid with a new one? I'm leaning towards yes. 
            while (Guid == Guid.Empty || manager.EntityExistsGlobaly(Guid)) //using a while here removes the infintisimal chance of creating a guid that already exsists. 
            {
                Guid = Guid.NewGuid();
            }

            Manager.SetupEntity(this, dataBlobs);

            /* Moved this to Manager.SetupEntity
            _protectedDataBlobMask_ = Manager.EntityMasks[ID];


            if (dataBlobs == null)
            {
                return;
            }

            foreach (BaseDataBlob dataBlob in dataBlobs)
            {
                if (dataBlob != null)
                {
                    SetDataBlob(dataBlob);
                }
            }
            */
        }

        internal Entity([NotNull] EntityManager manager, Guid factionID, [NotNull] ProtoEntity protoEntity) : this(protoEntity.Guid, manager, factionID, protoEntity.DataBlobs) { }
        internal Entity([NotNull] EntityManager manager, [NotNull] ProtoEntity protoEntity) : this(protoEntity.Guid, manager, Guid.Empty, protoEntity.DataBlobs) { }

        /// <summary>
        /// Sets the mask, called by the manager during SetupEntity.
        /// </summary>
        internal void SetMask()
        {
            _protectedDataBlobMask_ = Manager.EntityMasks[ID];
        }

        #endregion

        #region Public API Functions
        /// <summary>
        /// Used to determine if an entity is valid.
        /// 
        /// Entities are considered valid if they are not the static InvalidEntity and are properly registered to a manager.
        /// </summary>
        [PublicAPI]
        public bool IsValid => this != InvalidEntity && Manager != InvalidManager && Manager.IsValidEntity(this);


        /// <summary>
        /// Creates a new entity with a randomly generated ID, registered with the provided manager with the optionally provided dataBlobs.
        /// </summary>
        /// <returns>The create.</returns>
        /// <param name="manager">Manager.</param>
        /// <param name="faction">the faction owner of this new entity. use ID.Empty for a non owned entity.</param>
        /// <param name="dataBlobs">Data blobs.</param>
        [PublicAPI]

        public static Entity Create([NotNull] EntityManager manager, Guid faction, [CanBeNull] IEnumerable<BaseDataBlob> dataBlobs = null)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            return new Entity(manager, faction, dataBlobs);
        }

        /// <summary>
        /// Create an entity from specified manager, faction and protoEntity.
        /// </summary>
        /// <returns>The create.</returns>
        /// <param name="manager">Manager.</param>
        /// <param name="faction">the faction owner of this new entity. use ID.Empty for a non owned entity.</param>
        /// <param name="protoEntity">Proto entity.</param>
        public static Entity Create(EntityManager manager, Guid faction, ProtoEntity protoEntity)
        {
            return new Entity(protoEntity.Guid, manager, faction, protoEntity.DataBlobs);
        }

        [PublicAPI]
        public void Destroy()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot destroy an invalid entity.");
            }

            Manager.RemoveEntity(this);
            Manager = InvalidManager;
            _protectedDataBlobMask_ = EntityManager.BlankDataBlobMask();
        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Slower than GetDataBlob(int typeIndex)
        /// </summary>
        /// <typeparam name="T">Non-abstract derivative of BaseDataBlob</typeparam>
        [PublicAPI]
        public override T GetDataBlob<T>()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot get a datablob from an invalid entity.");
            }
            return Manager.GetDataBlob<T>(ID);
        }

        /// <summary>
        /// Direct lookup of an entity's DataBlob.
        /// Slower than directly accessing the DataBlob list.
        /// </summary>
        /// <typeparam name="T">Non-abstract derivative of BaseDataBlob</typeparam>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid typeIndex or entityID is passed.</exception>
        [PublicAPI]
        public override T GetDataBlob<T>(int typeIndex)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot get a datablob from an invalid entity.");
            }
            return Manager.GetDataBlob<T>(ID, typeIndex);
        }

        /// <summary>
        /// Sets the dataBlob to this entity. Slightly slower than SetDataBlob(dataBlob, typeIndex);
        /// </summary>
        /// <typeparam name="T">Non-abstract derivative of BaseDataBlob</typeparam>
        /// <exception cref="ArgumentNullException">Thrown is dataBlob is null.</exception>
        [PublicAPI]
        public override void SetDataBlob<T>([NotNull] T dataBlob)
        {
            if (dataBlob == null)
            {
                throw new ArgumentNullException(nameof(dataBlob), "Cannot use SetDataBlob to set a dataBlob to null. Use RemoveDataBlob instead.");
            }
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot set a datablob to an invalid entity.");
            }

            Manager.SetDataBlob(ID, dataBlob);
        }

        /// <summary>
        /// Sets the dataBlob to this entity. Slightly faster than SetDataBlob(dataBlob);
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown is dataBlob is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if typeIndex is not a valid typeIndex.</exception>
        [PublicAPI]
        public override void SetDataBlob<T>([NotNull] T dataBlob, int typeIndex)
        {
            if (dataBlob == null)
            {
                throw new ArgumentNullException(nameof(dataBlob), "Cannot use SetDataBlob to set a dataBlob to null. Use RemoveDataBlob instead.");
            }
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot set a datablob to an invalid entity.");
            }

            Manager.SetDataBlob(ID, dataBlob, typeIndex);
        }

        /// <summary>
        /// Removes a dataBlob from this entity. Slightly slower than RemoveDataBlob(typeIndex);
        /// </summary>
        /// <typeparam name="T">Non-abstract derivative of BaseDataBlob</typeparam>
        [PublicAPI]
        public override void RemoveDataBlob<T>()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot remove a datablob from an invalid entity.");
            }
            if (!HasDataBlob<T>())
            {
                throw new InvalidOperationException("Entity does not contain this datablob.");
            }
            Manager.RemoveDataBlob<T>(ID);
        }

        /// <summary>
        /// Removes a dataBlob from this entity. Slightly faster than the generic RemoveDataBlob(); function.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if typeIndex is not a valid typeIndex.</exception>
        [PublicAPI]
        public override void RemoveDataBlob(int typeIndex)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot remove a datablob from an invalid entity.");
            } 
            if (!HasDataBlob(typeIndex))
            {
                throw new InvalidOperationException("Entity does not contain this datablob.");
            }
            Manager.RemoveDataBlob(ID, typeIndex);
        }

        /// <summary>
        /// Checks if this entity has a DataBlob of type T.
        /// </summary>
        /// <typeparam name="T">Type of datablob to check for.</typeparam>
        /// <returns>True if the entity has the datablob.</returns>
        [PublicAPI]
        public bool HasDataBlob<T>() where T : BaseDataBlob
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot query an invalid entity.");
            }
            int typeIndex = EntityManager.GetTypeIndex<T>();
            return DataBlobMask[typeIndex];
        }

        /// <summary>
        /// Checks if this entity has a DataBlob of the type indicated by the provided typeIndex.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when typeIndex is not a valid typeIndex.</exception>
        /// <returns>True if the entity has the datablob.</returns>
        [PublicAPI]
        public bool HasDataBlob(int typeIndex)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot query an invalid entity.");
            }
            return DataBlobMask[typeIndex];
        }

        /// <summary>
        /// Clones the entity's dataBlobs into a new ProtoEntity.
        /// </summary>
        /// <returns>A new ProtoEntity with cloned datablobs from this entity.</returns>
        [PublicAPI]
        public ProtoEntity Clone()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot clone an invalid entity.");
            }
            List<BaseDataBlob> clonedDataBlobs = DataBlobs.Select(dataBlob => dataBlob.Clone()).Cast<BaseDataBlob>().ToList();
            return Create(Guid, clonedDataBlobs);
        }

        /// <summary>
        /// Clones the entity into a new Entity in the specified entit
        /// </summary>
        /// <param name="manager"></param>
        /// <returns>The newly created entity.</returns>
        [PublicAPI]
        public Entity Clone(EntityManager manager)
        {
            ProtoEntity clone = Clone();
            clone.Guid = Guid.NewGuid();
            return new Entity(manager, clone);
        }

        #endregion

        /// <summary>
        /// Simple override to display entities as their ID.
        /// 
        /// Used mostly in debugging.
        /// </summary>
        public override string ToString()
        {
            return Guid.ToString();
        }

        public override int GetValueCompareHash(int hash = 17)
        {
            foreach (var item in DataBlobs)
            {
                if (item is IGetValuesHash)
                    hash = ((IGetValuesHash)item).GetValueCompareHash(hash);
                else
                    hash = Misc.ValueHash(item, hash);
            }
            return hash;
        }

        public static PositionDB GetSOIParentPositionDB(Entity entity)
        {
            return (PositionDB)entity.GetDataBlob<PositionDB>().ParentDB;
        }

        public static Entity GetSOIParentEntity(Entity entity, PositionDB positionDB = null)
        {
            if (positionDB == null)
                positionDB = entity.GetDataBlob<PositionDB>();
            return positionDB.Parent;
        }

        public static (Vector3 pos, Vector3 Velocity) GetRalitiveState(Entity entity)
        {
            var pos = entity.GetDataBlob<PositionDB>().RelativePosition_m;
            if (entity.HasDataBlob<OrbitDB>())
            {
                var datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitDB>();
                
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector_m(orbit, datetime);
                return (pos, vel);
            }
            if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                var datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitUpdateOftenDB>();
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector_m(orbit, datetime);
                return (pos, vel);
            }

            if (entity.HasDataBlob<NewtonMoveDB>())
            {
                var move = entity.GetDataBlob<NewtonMoveDB>();
                
                var vel = move.CurrentVector_ms;
                return (pos, vel);
            }
            else
            {
                throw new Exception("Entity has no velocity");
            }
        }
        
        public static (Vector3 pos, Vector3 Velocity) GetAbsoluteState(Entity entity)
        {
            var posdb = entity.GetDataBlob<PositionDB>();
            var pos = posdb.AbsolutePosition_m;
            if (entity.HasDataBlob<OrbitDB>())
            {
                var datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitDB>();
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector_m(orbit, datetime);
                if (posdb.Parent != null)
                {
                    vel += GetAbsoluteState(posdb.Parent).Velocity;
                }

                return (pos, vel);
            }
            if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                var datetime = entity.StarSysDateTime;
                var orbit = entity.GetDataBlob<OrbitUpdateOftenDB>();
                var vel = OrbitProcessor.InstantaneousOrbitalVelocityVector_m(orbit, datetime);
                if (posdb.Parent != null)
                {
                    vel += GetAbsoluteState(posdb.Parent).Velocity;
                }
                return (pos, vel);
            }

            if (entity.HasDataBlob<NewtonMoveDB>())
            {
                var move = entity.GetDataBlob<NewtonMoveDB>();
                var vel = move.CurrentVector_ms;
                return (pos, vel);
            }
            else
            {
                throw new Exception("Entity has no velocity");
            }
        }
        
        
        

        public static Vector3 GetVelocity_m(Entity entity, DateTime atDateTime, bool ralitive = true)
        {
            if (entity.HasDataBlob<OrbitUpdateOftenDB>())
            {
                if(ralitive)
                    return OrbitProcessor.InstantaneousOrbitalVelocityVector_m(entity.GetDataBlob<OrbitUpdateOftenDB>(), atDateTime);
                else
                    return OrbitProcessor.AbsoluteOrbitalVector_m(entity.GetDataBlob<OrbitUpdateOftenDB>(), atDateTime);
            }

            if (entity.HasDataBlob<OrbitDB>())
            {
                if(ralitive)
                    return OrbitProcessor.InstantaneousOrbitalVelocityVector_m(entity.GetDataBlob<OrbitDB>(), atDateTime);
                else
                    return OrbitProcessor.AbsoluteOrbitalVector_m(entity.GetDataBlob<OrbitDB>(), atDateTime);
            }
            else if (entity.HasDataBlob<NewtonMoveDB>())
            {
                var vel = NewtonionMovementProcessor.GetRelativeState(entity, entity.GetDataBlob<NewtonMoveDB>(), atDateTime).vel;
                if (ralitive)
                    return vel;
                else //recurse
                    return GetVelocity_m(GetSOIParentEntity(entity), atDateTime, false) + vel;
            }
            else if (entity.HasDataBlob<WarpMovingDB>())
            {
                return entity.GetDataBlob<WarpMovingDB>().SavedNewtonionVector;
            }
            else
            {
                throw new Exception("Entity has no velocity");
            }
        }

        /// <summary>
        /// Gets a future position for this entity, regarless of wheter it's orbit or newtonion trajectory
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="atDateTime"></param>
        /// <param name="ralitive"></param>
        /// <returns>In Meters</returns>
        /// <exception cref="Exception"></exception>
        public static Vector3 GetPosition_m(Entity entity, DateTime atDateTime, bool ralitive = true)
        {
            if (entity.HasDataBlob<OrbitDB>())
            {
                if (ralitive)
                    return OrbitProcessor.GetPosition_m(entity.GetDataBlob<OrbitDB>(), atDateTime);
                else
                    return OrbitProcessor.GetAbsolutePosition_m(entity.GetDataBlob<OrbitDB>(), atDateTime);
            }
            else if (entity.HasDataBlob<NewtonMoveDB>())
            {
                if (ralitive)
                    return  NewtonionMovementProcessor.GetRelativeState(entity, entity.GetDataBlob<NewtonMoveDB>(), atDateTime).pos;
                else
                    return NewtonionMovementProcessor.GetAbsoluteState(entity, entity.GetDataBlob<NewtonMoveDB>(), atDateTime).pos;
                
            }
            else if (entity.HasDataBlob<PositionDB>())
            {
                if(ralitive)
                    return entity.GetDataBlob<PositionDB>().RelativePosition_m;
                else
                    return entity.GetDataBlob<PositionDB>().AbsolutePosition_m;
            }
            else
            {
                throw new Exception("Entity is positionless");
            }
        }

        /// <summary>
        /// Used to transfer an entity between managers.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when trying to transfer the static InvalidEntity.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the provided manager is null.</exception>
        public void Transfer([NotNull] EntityManager newManager)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot transfer an invalid entity.");
            }
            if (newManager == null)
            {
                throw new ArgumentNullException(nameof(newManager));
            }
            // Store dataBlobs.
            List<BaseDataBlob> dataBlobs = Manager.GetAllDataBlobsForEntity(ID);

            // Remove myself from the old manager.
            Manager.RemoveEntity(this);

            // Add myself the the new manager.
            Manager = newManager;
            newManager.SetupEntity(this, dataBlobs);

            /*
            _protectedDataBlobMask_ = Manager.EntityMasks[ID];

            foreach (BaseDataBlob dataBlob in dataBlobs)
            {
                SetDataBlob(dataBlob);
            }*/
        }


        /// <summary>
        /// EntityConverter is responsible for deserializing entities when they are encountered as references. 
        /// The EntityConverter must provide a proper reference to the object being deserialized.
        /// </summary>
        private class EntityConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Entity);
            }

            /// <summary>
            /// Returns a Entity object that represents the entity.
            /// If the Entity's manager has already deserialized the entity, then the EntityManager's reference is returned.
            /// If not, then we create the entity in the global manager, and when the EntityManager containing this entity deserializes,
            /// it will transfer the entity (that we create here) to itself. This will preserve all Entity references already deserialized.
            /// </summary>
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                Game game = (Game)serializer.Context.Context;
                Entity entity;

                // Parse the ID from the reader.
                Guid entityGuid = Guid.Parse(reader.Value.ToString());

                // Lookup the entity using a global ID lookup.
                if (entityGuid == Guid.Empty)
                    return InvalidEntity;
                if (game.GlobalManager.FindEntityByGuid(entityGuid, out entity))
                    return entity;

                // If no entity was found, create a new entity in the global manager. TODO: we need to get the FactionOwner guid from the json and use that here. 
                entity = new Entity(entityGuid, game.GlobalManager, Guid.Empty);
                return entity;
            }

            /// <summary>
            /// Serializes the Entity objects. Entities are serialized as simple Guids in this method.
            /// Datablobs are saved during EntityManager serialization.
            /// </summary>
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var entity = (Entity)value;
                serializer.Serialize(writer, entity.Guid);
            }
        }
    }
}
