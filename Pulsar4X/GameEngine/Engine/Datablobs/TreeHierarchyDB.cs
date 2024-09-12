using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Engine;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Datablobs
{
    /// <summary>
    /// Abstract base class for a DataBlob that acts within a tree hierarchy.
    /// </summary>
    /// <remarks>
    /// An example of this is our 2-body OrbitDB's.
    /// Earth's OrbitDB is a child of the Sun's OrbitDB in the tree hierarchy.
    ///
    /// Another example would be a subordinate fleet is a child to a higher-level fleet in the fleet heirarchy
    ///
    /// DataBlobs that derive from this type have functions to maintain the tree hierarchy as changes are made.
    /// </remarks>
    public abstract class TreeHierarchyDB : BaseDataBlob
    {
        [PublicAPI]
        public override Entity? OwningEntity
        {
            get { return _owningEntity_; }
            internal set
            {
                ParentDB?.RemoveChild(_owningEntity_);
                _owningEntity_ = value;

                if (OwningEntity != Entity.InvalidEntity && value != null)
                {
                    ParentDB?.AddChild(value);
                }
            }
        }

        [JsonProperty]
        protected Entity _owningEntity_;

        /// <summary>
        /// Parent node to this node.
        /// </summary>
        [CanBeNull]
        [PublicAPI]
        [JsonProperty]
        public Entity? Parent { get; private set; }

        /// <summary>
        /// Same type DataBlob of my parent node.
        /// </summary>
        /// <example>
        /// EarthOrbitDB.ParentDB == SunOrbitDB;
        /// </example>
        [CanBeNull]
        [PublicAPI]
        public TreeHierarchyDB? ParentDB
        {
            get
            {
                if (Parent == null)
                {
                    return null;
                }
                return Parent == Entity.InvalidEntity ? null : GetSameTypeDB(Parent);
            }
        }

        /// <summary>
        /// Root node of this tree hierachy.
        /// </summary>
        /// <example>
        /// LunaOrbitDB.Parent == Earth;
        /// LunaOrbitDB.Root = Sun;
        /// </example>
        [NotNull]
        [PublicAPI]
        public Entity Root => ParentDB?.Root ?? ((OwningEntity == null) ? Entity.InvalidEntity : OwningEntity);

        /// <summary>
        /// Same type DataBlob of my root node.
        /// </summary>
        [NotNull]
        [PublicAPI]
        public TreeHierarchyDB? RootDB => GetSameTypeDB(Root);

        /// <summary>
        /// All child nodes to this node.
        /// </summary>
        [NotNull]
        [PublicAPI]
        public SafeList<Entity> Children => _children;
        [JsonProperty]
        private readonly SafeList<Entity> _children;

        /// <summary>
        /// All node nodeDB's to this node.
        /// </summary>
        [NotNull]
        [PublicAPI]
        public List<TreeHierarchyDB?> ChildrenDBs => Children.Select(GetSameTypeDB).ToList();

        /// <summary>
        /// Creates a new TreeHierarchyDB with the provided parent.
        /// </summary>
        /// <param name="parent"></param>
        protected TreeHierarchyDB(Entity? parent)
        {
            Parent = parent;
            _children = new SafeList<Entity>();
        }

        /// <summary>
        /// Sets the parent of this node to another node, adjusting hierarchy as needed.
        /// </summary>
        /// <param name="parent"></param>
        internal virtual void SetParent(Entity? parent)
        {
            ParentDB?.RemoveChild(OwningEntity);
            Parent = parent;
            ParentDB?.AddChild(OwningEntity);
        }

        internal virtual void ClearParent()
        {
            Parent = null;
        }

        internal void AddChild(Entity? child)
        {
            if(child == null) return;
            if (Children.Contains(child))
            {
                return;
            }
            Children.Add(child);
            //Children.Sort((entity1, entity2) => entity1.ID.CompareTo(entity2.ID));
        }

        internal void RemoveChild(Entity? child)
        {
            if(child != null)
                Children.Remove(child);
        }

        public IEnumerable<Entity> GetChildren()
        {
            return Children.ToArray();
        }

        private TreeHierarchyDB? GetSameTypeDB(Entity entity)
        {
            return !entity.IsValid ? null : (TreeHierarchyDB)entity.GetDataBlob(this.GetType());
        }

        /*
        [TestFixture]
        [Description("TreeHierarchyDB Tests")]
        internal class TreeHierarchyTests
        {
            [TestUseOnly]
            private class ConcreteTreeHierarchyDB : TreeHierarchyDB
            {
                public ConcreteTreeHierarchyDB(Entity parent) : base(parent)
                {
                }

                public override object Clone()
                {
                    throw new NotImplementedException();
                }
            }

            private readonly EntityManager _manager = new EntityManager(new Game(), true);

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
                var parent1ChildEntities = new List<Entity> { parent1Child1, parent1Child2 };

                // Create a second set of children.
                Entity parent2Node = CreateNode(rootNode);
                Entity parent2Child1 = CreateNode(parent2Node);
                Entity parent2Child2 = CreateNode(parent2Node);

                // Store the second set of children.
                var parent2ChildEntities = new List<Entity> { parent2Child1, parent2Child2 };

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
                var parent1Children = new List<ConcreteTreeHierarchyDB> { parent1Child1DB, parent1Child2DB };
                var parent2Children = new List<ConcreteTreeHierarchyDB> { parent2Child1DB, parent2Child2DB };

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
                Assert.IsNotNull(parent2Child1DB.ParentDB);
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
        */
    }
}