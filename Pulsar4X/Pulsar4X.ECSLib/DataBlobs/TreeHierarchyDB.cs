using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Pulsar4X.ECSLib
{
    public abstract class TreeHierarchyDB : BaseDataBlob
    {
        [PublicAPI]
        public override Entity OwningEntity
        {
            get { return _owningEntity_; }
            internal set
            {
                if (ParentDB != null)
                {
                    ParentDB.RemoveChild(_owningEntity_);
                }
                _owningEntity_ = value;

                if (OwningEntity != Entity.InvalidEntity && ParentDB != null)
                {
                    ParentDB.AddChild(value);
                }
                
            }
        }
        protected Entity _owningEntity_;

        [CanBeNull]
        [PublicAPI]
        public Entity Parent
        {
            get { return _parent; }
            private set { _parent = value; }
        }
        [JsonProperty]
        private Entity _parent;

        [CanBeNull]
        [PublicAPI]
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
        [PublicAPI]
        public Entity Root
        {
            get
            {
                if (ParentDB != null)
                {
                    return ParentDB.Root;
                }
                if (OwningEntity != null)
                {
                    return OwningEntity;
                }
                throw new InvalidOperationException("TreeHierarchyDB cannot access Root entity before being assigned to an entity.");
            }
        }
        [NotNull]
        [PublicAPI]
        public TreeHierarchyDB RootDB
        {
            get { return GetSameTypeDB(Root); }
        }

        [NotNull]
        [PublicAPI]
        public List<Entity> Children
        {
            get { return _children; }
        }
        [JsonProperty]
        private readonly List<Entity> _children;

        [NotNull]
        [PublicAPI]
        public List<TreeHierarchyDB> ChildrenDBs
        {
            get { return Children.Select(GetSameTypeDB).ToList(); }
        }

        protected TreeHierarchyDB(Entity parent)
        {
            Parent = parent;
            _children = new List<Entity>();
        }

        internal void SetParent(Entity parent)
        {
            if (ParentDB != null)
            {
                ParentDB.RemoveChild(OwningEntity);
            }
            Parent = parent;
            if (ParentDB != null)
            {
                ParentDB.AddChild(OwningEntity);
            }
        }

        private void AddChild(Entity child)
        {
            if (child.Guid == Guid.Empty)
            {
                
            }
            if (Children.Contains(child))
            {
                return;
            }
            Children.Add(child);
            Children.Sort((entity1, entity2) => entity1.Guid.CompareTo(entity2.Guid));
        }

        private void RemoveChild(Entity child)
        {
            Children.Remove(child);
        }

        private TreeHierarchyDB GetSameTypeDB(Entity entity)
        {
            int typeIndex;
            EntityManager.TryGetTypeIndex(GetType(), out typeIndex);

            return entity.GetDataBlob<TreeHierarchyDB>(typeIndex);
        }

        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {
            OnDeserialized();
        }
        protected virtual void OnDeserialized()
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

        #region Unit Test

        private class ConcreteTreeHierarchyDB : TreeHierarchyDB
        {
            public ConcreteTreeHierarchyDB(Entity parent) : base(parent)
            {
            }

            private ConcreteTreeHierarchyDB() : base(null) { }

            public override object Clone()
            {
                throw new NotImplementedException();
            }
        }

        [TestFixture]
        [Description("TreeHierarchyDB Tests")]
        private class TreeHierarchyTestFixture
        {
            private readonly EntityManager _manager = new EntityManager(null);

            /// <summary>
            /// This test verifies the integrity of the TreeHierarchy datablob to maintain it's hierarchy during switches.
            /// </summary>
            [Test]
            public void TreeHierarchyTest()
            {
                // Create the "Root" entity.
                Entity rootNode = CreateNode(null);
                ConcreteTreeHierarchyDB rootDB = rootNode.GetDataBlob<ConcreteTreeHierarchyDB>();

                // Make sure the root has a root, itself.
                Assert.AreSame(rootDB, rootDB.RootDB);
                // Root doesn't have a parent.
                Assert.IsNull(rootDB.ParentDB);

                // Create a bunch of children.
                Entity parent1Node = CreateNode(rootNode);
                Entity parent1Child1 = CreateNode(parent1Node);
                Entity parent1Child2 = CreateNode(parent1Node);

                // Store a list of children for later comparison.
                var parent1ChildEntities = new List<Entity> {parent1Child1, parent1Child2};

                // Create a second set of children.
                Entity parent2Node = CreateNode(rootNode);
                Entity parent2Child1 = CreateNode(parent2Node);
                Entity parent2Child2 = CreateNode(parent2Node);

                // Store the second set of children.
                var parent2ChildEntities = new List<Entity> {parent2Child1, parent2Child2};

                // Get the dataBlobs of each child.
                ConcreteTreeHierarchyDB parent1DB = parent1Node.GetDataBlob<ConcreteTreeHierarchyDB>();
                ConcreteTreeHierarchyDB parent1Child1DB = parent1Child1.GetDataBlob<ConcreteTreeHierarchyDB>();
                ConcreteTreeHierarchyDB parent1Child2DB = parent1Child2.GetDataBlob<ConcreteTreeHierarchyDB>();

                ConcreteTreeHierarchyDB parent2DB = parent2Node.GetDataBlob<ConcreteTreeHierarchyDB>();
                ConcreteTreeHierarchyDB parent2Child1DB = parent2Child1.GetDataBlob<ConcreteTreeHierarchyDB>();
                ConcreteTreeHierarchyDB parent2Child2DB = parent2Child2.GetDataBlob<ConcreteTreeHierarchyDB>();

                // Ensure the root is the same across the branches.
                Assert.AreSame(rootDB, parent1Child1DB.RootDB);
                Assert.AreSame(rootDB, parent2Child2DB.RootDB);

                // Ensure children point to their parents.
                Assert.AreSame(parent1DB, parent1Child1DB.ParentDB);
                Assert.AreSame(parent2DB, parent2Child1DB.ParentDB);

                // Store a list of dataBlobs for later comparison.
                var parent1Children = new List<ConcreteTreeHierarchyDB> {parent1Child1DB, parent1Child2DB};
                var parent2Children = new List<ConcreteTreeHierarchyDB> {parent2Child1DB, parent2Child2DB};

                parent1ChildEntities.Sort((entity1, entity2) => entity1.Guid.CompareTo(entity2.Guid));
                parent2ChildEntities.Sort((entity1, entity2) => entity1.Guid.CompareTo(entity2.Guid));
                // Ensure listed child entities concur with our child list.
                Assert.AreEqual(parent1ChildEntities, parent1DB.Children);
                Assert.AreEqual(parent2ChildEntities, parent2DB.Children);

                parent1Children.Sort((entity1, entity2) => entity1.OwningEntity.Guid.CompareTo(entity2.OwningEntity.Guid));
                parent2Children.Sort((entity1, entity2) => entity1.OwningEntity.Guid.CompareTo(entity2.OwningEntity.Guid));
                // Ensure listen child DBs concur with our stored list.
                Assert.AreEqual(parent1Children, parent1DB.ChildrenDBs);
                Assert.AreEqual(parent2Children, parent2DB.ChildrenDBs);

                // Change P2C1's parent to P1.
                parent2Child1DB.SetParent(parent1DB.OwningEntity);
                // Make sure P2C1 is owned by P1;
                Debug.Assert(parent2Child1DB.ParentDB != null, "parent2Child1DB.ParentDB != null");
                Assert.AreEqual(parent1DB.OwningEntity, parent2Child1DB.ParentDB.OwningEntity);

                // Make sure P1's children list updated.
                parent1ChildEntities.Add(parent2Child1);
                parent1ChildEntities.Sort((entity1, entity2) => entity1.Guid.CompareTo(entity2.Guid));
                Assert.AreEqual(parent1ChildEntities, parent1DB.Children);

                // Make sure P2's children list updated.
                parent2ChildEntities.Remove(parent2Child1);
                Assert.AreEqual(parent2ChildEntities, parent2DB.Children);


                // Create a new root.
                Entity root2Node = CreateNode(null);
                ConcreteTreeHierarchyDB root2DB = root2Node.GetDataBlob<ConcreteTreeHierarchyDB>();

                // Assign P2 to the new root.
                parent2DB.SetParent(root2Node);
                // Make sure P2's child has a new RootDB.
                Assert.AreEqual(root2DB.OwningEntity, parent2Child2DB.RootDB.OwningEntity);

            }

            /// <summary>
            /// Helper function for TreeHierarchyTest
            /// </summary>
            private Entity CreateNode(Entity parentEntity)
            {
                ConcreteTreeHierarchyDB nodeDB = new ConcreteTreeHierarchyDB(parentEntity);
                Entity nodeEntity = new Entity(_manager);
                nodeEntity.SetDataBlob(nodeDB);

                return nodeEntity;
            }
        }
        #endregion

    }
}