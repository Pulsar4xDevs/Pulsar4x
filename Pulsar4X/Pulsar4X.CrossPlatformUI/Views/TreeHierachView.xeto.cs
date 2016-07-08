using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class TreeHierachView : Panel
    {
        protected TreeView TreeViewcontrol;

        private TreeHierarchyDB _rootDB;
        private Entity _rootEntity;

        public TreeHierachView()
        {
            XamlReader.Load(this);
        }
        public TreeHierachView(TreeHierarchyDB treeDB)
        { }

        void Init(EntityBlobPair ebPair)
        {
            
            List<TreeHierarchyDB> childBlobs = ebPair.Blob.ChildrenDBs;
            List<EntityBlobPair> children = new List<EntityBlobPair>();

            TreeItemCollection treeItemCollection = new TreeItemCollection();

            TreeItem treeitemroot = new TreeItem();
            treeitemroot.Text = ebPair.Entity.GetDataBlob<NameDB>().DefaultName;

            NewTreeItem(treeitemroot, ebPair);
                
            
            treeItemCollection.Add(treeitemroot);
            TreeViewcontrol.DataStore = treeItemCollection;
        }

        private TreeItem NewTreeItem(TreeItem parentTreeItem, EntityBlobPair ebPair)
        {
            TreeItem treeitem = new TreeItem();

            parentTreeItem.Text = ebPair.Entity.GetDataBlob<NameDB>().DefaultName;
            List<TreeHierarchyDB> childBlobs = ebPair.Blob.ChildrenDBs;
            List<EntityBlobPair> children = new List<EntityBlobPair>();
            
            foreach (var item in childBlobs)
            {
                
                var itemPair = new EntityBlobPair { Entity = item.Parent, Blob = item };
                children.Add(itemPair);
                parentTreeItem.Children.Add(NewTreeItem(treeitem, itemPair));
            }

            return treeitem;
        }

        struct EntityBlobPair
        {
            internal Entity Entity;
            internal TreeHierarchyDB Blob;
        }
    }
}
