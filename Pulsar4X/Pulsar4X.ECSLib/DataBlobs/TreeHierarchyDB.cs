using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
#if DEBUG
    abstract
#endif
    public class TreeHierarchyDB : BaseDataBlob
    {
        public Entity Parent
        {
            get { return _parent; }
            set
            {
                if (OwningEntity == null)
                {
                    _parent = value;
                    return;
                }

                if (Parent != null)
                {
                    ParentDB.RemoveChild(OwningEntity);
                }
                _parent = value;
                if (Parent != null)
                {
                    ParentDB.AddChild(OwningEntity);
                }
            }
        }
        private Entity _parent;

        [JsonIgnore]
        public TreeHierarchyDB ParentDB
        {
            get
            {
                if (Parent == null)
                    return null;

                return GetSameTypeDB(Parent);
            }
        }

        public Entity Root
        {
            get
            {
                if (Parent == null)
                {
                    return OwningEntity;
                }

                return ParentDB.Root;
            }
        }

        [JsonIgnore]
        public TreeHierarchyDB RootDB
        {
            get { return GetSameTypeDB(Root); }
        }

        public List<Entity> Children { get; private set; }

        [JsonIgnore]
        public List<TreeHierarchyDB> ChildrenDBs
        {
            get { return Children.Select(GetSameTypeDB).ToList(); }
        }

        public override Entity OwningEntity
        {
            get { return _owningEntity; }
            set
            {
                if (_owningEntity != null && Parent != null)
                {
                    ParentDB.RemoveChild(_owningEntity);
                }

                _owningEntity = value;

                if (Parent != null)
                {
                    ParentDB.AddChild(_owningEntity);
                }
            }
        }
        private Entity _owningEntity;

        public TreeHierarchyDB(Entity parent)
        {
            Parent = parent;
            Children = new List<Entity>();
        }

        private void AddChild(Entity child)
        {
            Children.Add(child);
        }

        private bool RemoveChild(Entity child)
        {
            return Children.Remove(child);
        }

        private TreeHierarchyDB GetSameTypeDB(Entity entity)
        {
            if (!entity.IsValid)
            {
                throw new ArgumentException("Invalid Entity");
            }

            int typeIndex;
            EntityManager.TryGetTypeIndex(GetType(), out typeIndex);

            return entity.GetDataBlob<TreeHierarchyDB>(typeIndex);
        }
    }
}