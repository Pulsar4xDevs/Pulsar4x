using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class SysInfo1dMap : Drawable
    {
        private IconCollection _iconCollection = new IconCollection();
        Camera2dv2 _cam;
        private SystemInfoVM _vm;
        private List<int> _widths;
        private List<int> _heights;
        public SysInfo1dMap()
        {
            XamlReader.Load(this);
            DataContextChanged += SysInfo1dMap_DataContextChanged;
            _cam = new Camera2dv2(this);
            _cam.ZoomLevel = 1;
        }

        private void SysInfo1dMap_DataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is SystemInfoVM)
            {
                _vm = (SystemInfoVM)DataContext;
                _vm.StarSystems.SelectionChangedEvent += StarSystems_SelectionChangedEvent;
                _vm.Entities.SelectionChangedEvent += Entities_SelectionChangedEvent;
                _vm.TreeBlobs.SelectionChangedEvent += TreeBlobs_SelectionChangedEvent;
                Init();
            }

        }

        private void Entities_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            Init();
        }

        private void TreeBlobs_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            Init();
        }

        private void StarSystems_SelectionChangedEvent(int oldSelection, int newSelection)
        {
            Invalidate();
        }

        private void Init()
        {

            
            _iconCollection.Init(_vm.Entities.Keys, _cam);
        }



        protected override void OnPaint(PaintEventArgs e)
        {
            if (_cam != null)
            {
                _cam.WorldOffset(new PointF(-Width / 2, -Height / 2));
            }
                // your custom drawing
                e.Graphics.FillRectangle(Colors.Black, e.ClipRectangle);
                int xIndex = 1;
                int yIndex = 1;
                var icon = _iconCollection.IconDict[_vm.EBTreePair.Entity.Guid];

                icon.DrawMe(e.Graphics, new PointF(8 * xIndex, 8 * yIndex));

                foreach (var item in _vm.EBTreePair.Blob.ChildrenDBs)
                {
                    xIndex++;               
                    RecursiveDraw(e.Graphics, item, xIndex, yIndex);
                }
            
        }

        private void RecursiveDraw(Graphics g, ECSLib.TreeHierarchyDB dblob,  int xIndex, int yIndex)
        {
            

                xIndex += 1;
                var icon = _iconCollection.IconDict[dblob.OwningEntity.Guid];
                icon.DrawMe(g, new PointF(16 * xIndex, 16 * yIndex));
                foreach (var Yitem in dblob.ChildrenDBs)
                {
                    yIndex += 1;
                    RecursiveDraw(g, Yitem, xIndex, yIndex);
                }

            
            
        }
    }
}
