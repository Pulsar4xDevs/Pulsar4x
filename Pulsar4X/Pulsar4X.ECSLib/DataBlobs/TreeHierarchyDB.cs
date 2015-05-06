using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public abstract class TreeHierarchyDB : BaseDataBlob
    {
        [CanBeNull]
        public Entity Parent
        {
            get { return _parent; }
            set
            {
                if (Equals(value, _parent))
                {
                    return;
                }

                _parent = value;

                OnPropertyChanged();

                if (Parent != null && OwningEntity != null)
                {
                    ParentDB.AddChild(OwningEntity);
                }
            }
        }
        private Entity _parent;

        [JsonIgnore]
        [CanBeNull]
        public TreeHierarchyDB ParentDB
        {
            get
            {
                if (Parent == null)
                    return null;

                return GetSameTypeDB(Parent);
            }
        }

        [NotNull]
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
        [NotNull]
        public TreeHierarchyDB RootDB
        {
            get { return GetSameTypeDB(Root); }
        }

        [NotNull]
        public List<Entity> Children { get; private set; }

        [JsonIgnore]
        [NotNull]
        public List<TreeHierarchyDB> ChildrenDBs
        {
            get { return Children.Select(GetSameTypeDB).ToList(); }
        }

        [CanBeNull]
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

        protected TreeHierarchyDB(Entity parent)
        {
            Parent = parent;
            Children = new List<Entity>();
        }

        private void AddChild(Entity child)
        {
            Children.Add(child);
            GetSameTypeDB(child).PropertyChanged += OnPropertyChanged;
            OnPropertyChanged("Children");
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "Parent")
            {
                RemoveChild(((TreeHierarchyDB)sender).OwningEntity);
            }
        }

        private bool RemoveChild(Entity child)
        {
            bool removed = Children.Remove(child);
            if (!removed)
            {
                return false;
            }

            // Unsubscribe from the event.
            GetSameTypeDB(child).PropertyChanged -= OnPropertyChanged;

            // Fire our property changed event.
            OnPropertyChanged("Children");

            return true;
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

    /// <summary>
    /// For use by Unit Tests only.
    /// </summary>
    public sealed class ConcreteTreeHierarchyDB : TreeHierarchyDB
    {
        public ConcreteTreeHierarchyDB() 
            : base(null)
        {

        }

        public ConcreteTreeHierarchyDB(Entity parent)
            : base(parent)
        {

        }

        public ConcreteTreeHierarchyDB(ConcreteTreeHierarchyDB concreteTreeHierarchyDB)
            : base(concreteTreeHierarchyDB.Parent)
        {
        }

        public override object Clone()
        {
            return new ConcreteTreeHierarchyDB(this);
        }
    }
}