using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Eto.Drawing;

namespace Pulsar4X.CrossPlatformUI.Views
{
    internal class IconCollection
    {
        List<IconBase> Icons { get; } = new List<IconBase>();

        public IconCollection()
        {


        }

        public void Init(List<Entity> entities, Camera2dv2 camera)
        {
            Icons.Clear();
            foreach (var item in entities)
            {
                if (item.HasDataBlob<OrbitDB>() && item.GetDataBlob<OrbitDB>().Parent != null)
                {
                    OrbitRing ring = new OrbitRing(item, camera);

                    Icons.Add(ring);
                }
                if (item.HasDataBlob<NameDB>())
                    Icons.Add(new TextIcon(item, camera));

                Icons.Add(new EntityIcon(item, camera));
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
