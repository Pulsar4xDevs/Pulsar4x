using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("DataBlob Tests")]
    internal class DataBlobTests
    {
        private static readonly List<Type> DataBlobTypes = new List<Type>(Assembly.GetAssembly(typeof(BaseDataBlob)).GetTypes().Where(type => type.IsSubclassOf(typeof(BaseDataBlob)) && !type.IsAbstract));
        private static EntityManager _manager = new EntityManager();

        [SetUp]
        public void Init()
        {
            _manager = new EntityManager();
        }

        [Test]
        public void TypeCount()
        {
            Assert.AreEqual(DataBlobTypes.Count, EntityManager.BlankDataBlobMask().Length);
        }

        [Test]
        [TestCaseSource("DataBlobTypes")]
        public void DeepCopyConstructor(Type dataBlobType)
        {
            ConstructorInfo constructor = dataBlobType.GetConstructor(new[] {dataBlobType});
            if (constructor == null)
            {
                Assert.Fail(dataBlobType + " does not have a Deep Copy constructor.");
            }
        }

        [Test]
        public void TreeHierarchyTest()
        {
            Entity rootNode = CreateNode(null);
            TreeHierarchyDB rootDB = rootNode.GetDataBlob<TreeHierarchyDB>();

            Assert.AreSame(rootDB, rootDB.RootDB);

            Assert.IsNull(rootDB.ParentDB);

            Entity Parent1_node = CreateNode(rootNode);
            Entity Parent1_child1 = CreateNode(Parent1_node);
            Entity Parent1_child2 = CreateNode(Parent1_node);

            var Parent1_childEntitys = new List<Entity> { Parent1_child1, Parent1_child2 };

            Entity Parent2_node = CreateNode(rootNode);
            Entity Parent2_child1 = CreateNode(Parent2_node);
            Entity Parent2_child2 = CreateNode(Parent2_node);

            var Parent2_childEntitys = new List<Entity> { Parent2_child1, Parent2_child2 };

            TreeHierarchyDB Parent1DB = Parent1_node.GetDataBlob<TreeHierarchyDB>();
            TreeHierarchyDB Parent1_child1DB = Parent1_child1.GetDataBlob<TreeHierarchyDB>();
            TreeHierarchyDB Parent1_child2DB = Parent1_child2.GetDataBlob<TreeHierarchyDB>();

            TreeHierarchyDB Parent2DB = Parent2_node.GetDataBlob<TreeHierarchyDB>();
            TreeHierarchyDB Parent2_child1DB = Parent2_child1.GetDataBlob<TreeHierarchyDB>();
            TreeHierarchyDB Parent2_child2DB = Parent2_child2.GetDataBlob<TreeHierarchyDB>();

            Assert.AreSame(rootDB, Parent1_child1DB.RootDB);
            Assert.AreSame(rootDB, Parent2_child2DB.RootDB);
            Assert.AreSame(Parent1DB, Parent1_child1DB.ParentDB);
            Assert.AreSame(Parent2DB, Parent2_child1DB.ParentDB);

            var Parent1_children = new List<TreeHierarchyDB> { Parent1_child1DB, Parent1_child2DB };
            var Parent2_children = new List<TreeHierarchyDB> { Parent2_child1DB, Parent2_child2DB };

            Assert.AreEqual(Parent1DB.Children, Parent1_childEntitys);
            Assert.AreEqual(Parent2DB.Children, Parent2_childEntitys);

            Assert.AreEqual(Parent1_children, Parent1DB.ChildrenDBs);
            Assert.AreEqual(Parent2_children, Parent2DB.ChildrenDBs);

            Parent2_child1DB.Parent = Parent1DB.OwningEntity;
            Assert.AreEqual(Parent1DB.OwningEntity, Parent2_child1DB.ParentDB.OwningEntity);

            Entity root2Node = CreateNode(null);
            TreeHierarchyDB root2DB = root2Node.GetDataBlob<TreeHierarchyDB>();

            Parent2DB.Parent = root2Node;
            Assert.AreEqual(root2DB.OwningEntity, Parent2_child2DB.RootDB.OwningEntity);

        }

        private Entity CreateNode(Entity parentEntity)
        {
            TreeHierarchyDB nodeDB = new TreeHierarchyDB(parentEntity);
            Entity nodeEntity = Entity.Create(_manager);
            nodeEntity.SetDataBlob(nodeDB);

            return nodeEntity;
        }
    }
}