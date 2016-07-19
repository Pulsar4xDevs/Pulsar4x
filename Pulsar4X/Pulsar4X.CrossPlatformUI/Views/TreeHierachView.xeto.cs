using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel;

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
            DataContextChanged += TreeHierachView_DataContextChanged;
        }

        private void TreeHierachView_DataContextChanged(object sender, EventArgs e)
        {
            if(DataContext is EntityBlobPair)
                Init();
        }

        void Init()
        {
            EntityBlobPair ebpair = (EntityBlobPair)DataContext;
            List<TreeHierarchyDB> childBlobs = ebpair.Blob.ChildrenDBs;

            TreeItemCollection treeItemCollection = new TreeItemCollection();

            TreeItem treeitemroot = new TreeItem();
            treeitemroot.Text = ebpair.Entity.GetDataBlob<NameDB>().DefaultName;

            NewTreeItem(treeitemroot, ebpair);
                
            
            treeItemCollection.Add(treeitemroot);
            TreeViewcontrol.DataStore = treeItemCollection;
        }

        private void NewTreeItem(TreeItem parentTreeItem, EntityBlobPair ebPair)
        {                       
            List<TreeHierarchyDB> childBlobs = ebPair.Blob.ChildrenDBs;
                      
            foreach (var item in childBlobs)
            {
                TreeItem treeitem = new TreeItem();              
                var itemPair = new EntityBlobPair { Entity = item.OwningEntity, Blob = item };
                treeitem.Text = itemPair.Entity.GetDataBlob<NameDB>().DefaultName;
                parentTreeItem.Children.Add(treeitem);
                NewTreeItem(treeitem, itemPair);
            }            
        }
    }
}
