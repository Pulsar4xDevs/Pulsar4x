using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Pulsar4X.ECSLib.DataBlobs
{
    public abstract class TreeHierarchyDB : BaseDataBlob
    {
        private readonly List<TreeHierarchyDB> _childDBs;
        private readonly List<Guid> _childGuids;
        private readonly object _internalLock;
        private readonly int _typeIndex;
        private Guid _parentGuid;

        /// <summary>
        /// A datablob type that automatically resolves references for a tree heirarchy.
        /// </summary>
        protected TreeHierarchyDB()
        {
            _internalLock = new object();

            EntityManager.TryGetTypeIndex(GetType(), out _typeIndex);

            _childDBs = new List<TreeHierarchyDB>();
            _childGuids = new List<Guid>();
        }

        /// <summary>
        /// Used to resolve all Guid references after deserialization.
        /// </summary>
        public void ResolveGuids()
        {
            ParentDB = null;

            ParentDB = FindSameTypeDB(ParentGuid, _typeIndex);

            foreach (Guid childGuid in _childGuids)
            {
                _childDBs.Add(FindSameTypeDB(childGuid, _typeIndex));
            }
        }

        /// <summary>
        /// Parent datablob of this datablob in the hierarchy.
        /// </summary>
        public TreeHierarchyDB ParentDB { get; private set; }

        /// <summary>
        /// Root datablob of the hierarchy.
        /// </summary>
        public TreeHierarchyDB RootDB { get; private set; }

        /// <summary>
        /// A read-only list of child datablobs.
        /// </summary>
        public ReadOnlyCollection<TreeHierarchyDB> ChildDBs
        {
            get
            {
                lock (_internalLock)
                {
                    return _childDBs.AsReadOnly();
                }
            }
        }

        /// <summary>
        /// The Guid of this datablob's parent.
        /// </summary>
        public Guid ParentGuid
        {
            get { return _parentGuid; }
            set
            {
                lock (_internalLock)
                {
                    _parentGuid = value;
                    if (value != Guid.Empty)
                    {
                        ParentDB = FindSameTypeDB(value, _typeIndex);
                        RootDB = ParentDB.RootDB;
                    }
                    else
                    {
                        ParentDB = null;
                        RootDB = this;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a child to this datablob hierarchy.
        /// </summary>
        /// <param name="childGuid">Guid of child to add.</param>
        public void AddChild(Guid childGuid)
        {
            lock (_internalLock)
            {
                TreeHierarchyDB child = FindSameTypeDB(childGuid, _typeIndex);
                _childGuids.Add(childGuid);
                _childDBs.Add(child);
            }
        }

        /// <summary>
        /// Removes a child from this datablob hierarchy.
        /// </summary>
        /// <param name="childGuid">Guid of child to remove.</param>
        public void RemoveChild(Guid childGuid)
        {
            lock (_internalLock)
            {
                TreeHierarchyDB child = FindSameTypeDB(childGuid, _typeIndex);
                _childGuids.Remove(childGuid);
                _childDBs.Remove(child);
            }
        }

        private static TreeHierarchyDB FindSameTypeDB(Guid entityGuid, int typeIndex)
        {
            EntityManager entityManager;
            int entityID;

            EntityManager.FindEntityByGuid(entityGuid, out entityManager, out entityID);

            return entityManager.GetDataBlob<TreeHierarchyDB>(entityID, typeIndex);
        }
    }
}