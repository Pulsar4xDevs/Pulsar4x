using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public abstract class TreeHierarchyDB : BaseDataBlob
    {
        private readonly List<TreeHierarchyDB> _childDBs;
        private readonly object _internalLock;
        private readonly int _typeIndex;
        private TreeHierarchyDB _parentDB;
        private TreeHierarchyDB _rootDB;

        protected TreeHierarchyDB(Guid parentGuid)
        {
            _internalLock = new object();

            EntityManager.TryGetTypeIndex(GetType(), out _typeIndex);
            ParentGuid = parentGuid;

            ChildGuids = new List<Guid>();
            _childDBs = new List<TreeHierarchyDB>();
        }

        protected TreeHierarchyDB(Guid parentGuid, List<Guid> childGuids) : this(parentGuid)
        {
            ChildGuids = childGuids;

            foreach (Guid childGuid in ChildGuids)
            {
                _childDBs.Add(FindSameTypeDB(childGuid));
            }
        }

        public Guid ParentGuid { get; set; }
        public List<Guid> ChildGuids { get; set; }

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
            private set { _rootDB = value; }
        }

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
            private set { _parentDB = value; }
        }

        public ReadOnlyCollection<TreeHierarchyDB> ChildDBs
        {
            get { return _childDBs.AsReadOnly(); }
        }

        public override Guid EntityGuid
        {
            get { return base.EntityGuid; }
            set
            {
                if (ParentDB != null)
                {
                    int index = ParentDB.ChildGuids.FindIndex((Guid listValue) => listValue == EntityGuid);
                    if (index != -1)
                    {
                        index = ParentDB.ChildGuids.IndexOf(EntityGuid);
                        ParentDB.ChildGuids[index] = value;
                    }
                    else
                    {
                        ParentDB.ChildGuids.Add(value);
                    }
                }
                base.EntityGuid = value;
            }
        }

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
    }
}