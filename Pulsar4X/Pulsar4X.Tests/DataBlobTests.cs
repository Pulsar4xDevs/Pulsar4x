using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [Description("DataBlob Tests")]
    class DataBlobTests
    {
        private List<Type> _dataBlobTypes;
        private EntityManager _manager;

        [SetUp]
        public void Init()
        {
            _manager = new EntityManager();
            //_dataBlobTypes = new List<Type>(Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(BaseDataBlob)) && !type.IsAbstract));
        }

#if DEBUG
        [Test]
        public void TreeHierarchyTest()
        {
            Assert.Catch<GuidNotFoundException>(() =>
            {
                Guid newNode = CreateNode(Guid.NewGuid());
            });

            Guid rootNode = CreateNode(Guid.Empty);
            TreeHierarchyDBConcrete rootDB = GetNodeDataBlob(rootNode);

            Assert.AreSame(rootDB, rootDB.RootDB);

            Assert.IsNull(rootDB.ParentDB);

            Guid Parent1_node = CreateNode(rootNode);
            Guid Parent1_child1 = CreateNode(Parent1_node);
            Guid Parent1_child2 = CreateNode(Parent1_node);

            var Parent1_childGuids = new List<Guid> {Parent1_child1, Parent1_child2};

            Guid Parent2_node = CreateNode(rootNode);
            Guid Parent2_child1 = CreateNode(Parent2_node);
            Guid Parent2_child2 = CreateNode(Parent2_node);

            var Parent2_childGuids = new List<Guid> { Parent2_child1, Parent2_child2 };

            TreeHierarchyDBConcrete Parent1DB = GetNodeDataBlob(Parent1_node);
            TreeHierarchyDBConcrete Parent1_child1DB = GetNodeDataBlob(Parent1_child1);
            TreeHierarchyDBConcrete Parent1_child2DB = GetNodeDataBlob(Parent1_child2);

            TreeHierarchyDBConcrete Parent2DB = GetNodeDataBlob(Parent2_node);
            TreeHierarchyDBConcrete Parent2_child1DB = GetNodeDataBlob(Parent2_child1);
            TreeHierarchyDBConcrete Parent2_child2DB = GetNodeDataBlob(Parent2_child2);

            Assert.AreSame(rootDB, Parent1_child1DB.RootDB);
            Assert.AreSame(rootDB, Parent2_child2DB.RootDB);
            Assert.AreSame(Parent1DB, Parent1_child1DB.ParentDB);
            Assert.AreSame(Parent2DB, Parent2_child1DB.ParentDB);

            var Parent1_children = new List<TreeHierarchyDBConcrete> { Parent1_child1DB, Parent1_child2DB };
            var Parent2_children = new List<TreeHierarchyDBConcrete> { Parent2_child1DB, Parent2_child2DB };

            Assert.AreEqual(Parent1DB.ChildGuids, Parent1_childGuids);
            Assert.AreEqual(Parent2DB.ChildGuids, Parent2_childGuids);

            Assert.AreEqual(Parent1_children, Parent1DB.ChildDBs.Cast<TreeHierarchyDBConcrete>().ToList());
            Assert.AreEqual(Parent2_children, Parent2DB.ChildDBs.Cast<TreeHierarchyDBConcrete>().ToList());

            Parent2_child1DB.SetParent(Parent1DB.EntityGuid);
            Assert.AreEqual(Parent1DB.EntityGuid, Parent2_child1DB.ParentDB.EntityGuid);

            Guid root2Node = CreateNode(Guid.Empty);
            TreeHierarchyDBConcrete root2DB = GetNodeDataBlob(root2Node);

            Parent2DB.SetParent(root2Node);
            Assert.AreEqual(root2DB.EntityGuid, Parent2_child2DB.RootDB.EntityGuid);

        }

        private Guid CreateNode(Guid parentGuid)
        {
            TreeHierarchyDBConcrete nodeDB = new TreeHierarchyDBConcrete(parentGuid);
            int nodeEntity = _manager.CreateEntity();
            _manager.SetDataBlob(nodeEntity, nodeDB);

            Guid nodeGuid;
            if (!_manager.TryGetGuidByEntity(nodeEntity, out nodeGuid))
                throw new GuidNotFoundException();

            return nodeGuid;
        }

        private TreeHierarchyDBConcrete GetNodeDataBlob(Guid nodeGuid)
        {
            int entityID;
            _manager.TryGetEntityByGuid(nodeGuid, out entityID);
            return _manager.GetDataBlob<TreeHierarchyDBConcrete>(entityID);
        } 
#endif
    }
}