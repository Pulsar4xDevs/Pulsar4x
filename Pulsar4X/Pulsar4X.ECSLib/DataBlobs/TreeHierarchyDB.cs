using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
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
        
        [JsonIgnore]
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
        }

        private void AddChild(Entity child)
        {
            Children.Add(child);
            GetSameTypeDB(child).PropertyChanged += OnPropertyChanged;
        }

        private void RemoveChild(Entity child)
        {
            bool removed = Children.Remove(child);
            if (!removed)
            {
                return;
            }

            // Unsubscribe from the event.
            GetSameTypeDB(child).PropertyChanged -= OnPropertyChanged;
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
            int typeIndex;
            EntityManager.TryGetTypeIndex(GetType(), out typeIndex);

            return entity.GetDataBlob<TreeHierarchyDB>(typeIndex);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SaveGame.CurrentGame.PostLoad += PostLoadHandler;
        }

        private void PostLoadHandler(object sender, EventArgs e)
        {
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
        public ConcreteTreeHierarchyDB(Entity parent)
            : base(parent)
        {
        }

        public override object Clone()
        {
            return new ConcreteTreeHierarchyDB(Parent);
        }
    }
}