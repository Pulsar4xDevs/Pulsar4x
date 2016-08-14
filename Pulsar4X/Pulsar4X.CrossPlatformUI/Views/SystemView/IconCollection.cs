using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Eto.Drawing;
using System;

namespace Pulsar4X.CrossPlatformUI.Views
{
    internal class IconCollection
    {
        public List<IconBase> Icons { get; } = new List<IconBase>();
        public Dictionary<Guid, EntityIcon> IconDict { get; } = new Dictionary<Guid, EntityIcon> ();
        public IconCollection()
        {
        }

        public void Init(IEnumerable<Entity> entities, Camera2dv2 camera)
        {
            Icons.Clear();
            IconDict.Clear();
            foreach (var item in entities)
            {
                if (item.HasDataBlob<OrbitDB>() && item.GetDataBlob<OrbitDB>().Parent != null)
                {
                    OrbitRing ring = new OrbitRing(item, camera);

                    Icons.Add(ring);
                }
                if (item.HasDataBlob<NameDB>())
                    Icons.Add(new TextIcon(item, camera));

                EntityIcon entIcon = new EntityIcon(item, camera);
                Icons.Add(entIcon);
                IconDict.Add(item.Guid, entIcon);
            }
        }

        public void DrawMe(Graphics g)
        {
            foreach (var item in Icons)
            {
                item.DrawMe(g);
            }
        }
    }

    internal interface IconBase
    {
        //sets the size of the icons
        float Scale { get; set; }

        void DrawMe(Graphics g);
    }

}
