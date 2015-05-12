using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public abstract class TreeHierarchyDB : BaseDataBlob, IPostLoad
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

                if (ParentDB != null && OwningEntity != null)
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
                if (ParentDB == null)
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

        protected TreeHierarchyDB(Entity parent)
        {
            Parent = parent;
            Children = new List<Entity>();

            PropertyChanged += OnPropertyChanged;
            Game.Instance.PostLoad += PostLoad;
        }

        private void AddChild(Entity child)
        {
            Children.Add(child);
            GetSameTypeDB(child).PropertyChanged += OnPropertyChanged;
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

            return true;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (propertyChangedEventArgs.PropertyName == "Parent")
            {
                RemoveChild(((TreeHierarchyDB)sender).OwningEntity);
            }
            else if (propertyChangedEventArgs.PropertyName == "OwningEntity")
            {
                if (ParentDB != null)
                {
                    ParentDB.AddChild(OwningEntity);
                }
            }
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

        public void PostLoad(object sender, EventArgs e)
        {
            Game.Instance.PostLoad -= PostLoad;

            if (Parent != null && ParentDB != null && OwningEntity != null)
            {
                ParentDB.AddChild(OwningEntity);
            }
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