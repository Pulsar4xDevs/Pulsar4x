using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public abstract class TreeHierarchyDB : BaseDataBlob
    {
        private readonly int _typeIndex;

        private TreeHierarchyDB()
        {
            EntityManager.TryGetTypeIndex(GetType(), out _typeIndex);
        }

        protected TreeHierarchyDB(Guid parentGuid) : this()
        {
            ParentGuid = parentGuid;
            if (parentGuid != Guid.Empty)
            {
                _parentDB = FindSameTypeDB(ParentGuid);
            }

            _childGuids = new List<Guid>();
            _childDBs = new List<TreeHierarchyDB>();
        }

        public Guid ParentGuid { get; private set; }
        [JsonIgnore]
        public TreeHierarchyDB ParentDB
        {
            get
            {
                if (_parentDB == null && ParentGuid != Guid.Empty)
                {
                    _parentDB = FindSameTypeDB(ParentGuid);
                }

                return _parentDB;
            }
        }
        private TreeHierarchyDB _parentDB;

        [JsonIgnore]
        public TreeHierarchyDB RootDB
        {
            get
            {
                if (_rootDB == null)
                {
                    if (ParentDB == null)
                    {
                        _rootDB = this;
                    }
                    else
                    {
                        _rootDB = ParentDB.RootDB;
                    }
                }
                return _rootDB;
            }
        }
        private TreeHierarchyDB _rootDB;


        [JsonIgnore]
        public ReadOnlyCollection<Guid> ChildGuids
        {
            get { return _childGuids.AsReadOnly(); }
        }
        [JsonProperty("ChildGuids")]
        private readonly List<Guid> _childGuids;

        [JsonIgnore]
        public ReadOnlyCollection<TreeHierarchyDB> ChildDBs
        {
            get { return _childDBs.AsReadOnly(); }
        }
        private readonly List<TreeHierarchyDB> _childDBs;

        [JsonIgnore]
        public override Guid EntityGuid
        {
            get { return _entityGuid; }
            set
            {
                if (ParentDB != null)
                {
                    ParentDB.AddChild(value);
                }

                _entityGuid = value;
            }
        }
        [JsonProperty("EntityGuid")]
        private Guid _entityGuid;

        private TreeHierarchyDB FindSameTypeDB(Guid entityGuid)
        {
            EntityManager entityManager;
            int entityID;

            if (!EntityManager.FindEntityByGuid(entityGuid, out entityManager, out entityID))
            {
                throw new GuidNotFoundException();
            }

            return entityManager.GetDataBlob<TreeHierarchyDB>(entityID, _typeIndex);
        }

        public void SetParent(Guid parentGuid)
        {
            // Unlink us from current parent.
            if (ParentDB != null)
            {
                ParentDB.RemoveChild(EntityGuid);
            }

            // Link us to the parent.
            ParentGuid = parentGuid;

            // Link parent back to us.
            if (parentGuid != Guid.Empty)
            {
                _parentDB = FindSameTypeDB(parentGuid);
                _parentDB.AddChild(EntityGuid);
            }
            InvalidateRoot();
        }

        private void InvalidateRoot()
        {
            _rootDB = null;
            foreach (TreeHierarchyDB child in ChildDBs)
            {
                child.InvalidateRoot();
            }
        }

        private void AddChild(Guid childGuid)
        {
            TreeHierarchyDB childDB = FindSameTypeDB(childGuid);
            _childDBs.Add(childDB);
            _childGuids.Add(childGuid);
        }

        private void RemoveChild(Guid childGuid)
        {
            TreeHierarchyDB childDB = FindSameTypeDB(childGuid);
            _childDBs.Remove(childDB);
            _childGuids.Remove(childGuid);
        }
    }

#if DEBUG
    // Non-abstract class to test the TreeHierarchyDB class.
    public class TreeHierarchyDBConcrete : TreeHierarchyDB
    {
        public TreeHierarchyDBConcrete(Guid parentGuid) : base(parentGuid)
        {
        }
    }
#endif
}