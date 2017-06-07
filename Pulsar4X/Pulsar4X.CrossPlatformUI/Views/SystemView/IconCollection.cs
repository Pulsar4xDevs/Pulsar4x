using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Eto.Drawing;
using System;

namespace Pulsar4X.CrossPlatformUI.Views
{
    internal class IconCollection
    {
        //Main dictionaries for storing Icons
        //The Guid represents the entity, the Icon is created for
        public List<OrbitRing> OrbitList { get; } = new List<OrbitRing>();
        public List<TextIcon> TextIconList { get; } = new List<TextIcon>();
        public List<EntityIcon> EntityList { get; } = new List<EntityIcon>();
        public ScaleIcon Scale { get; set; }


        public Dictionary<Guid, EntityIcon> IconDict { get; } = new Dictionary<Guid, EntityIcon> ();

        /// <summary>
        /// Constructs an IconCollection
        /// </summary>
        public IconCollection()
        {
        }

        /// <summary>
        /// Initializes the IconCollection
        /// Creates Orbitrings, TextIcons and Entityicons and adds them to Collection
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="camera"></param>
        public void Init(IEnumerable<Entity> entities, Camera2dv2 camera)
        {
            IconDict.Clear();
            OrbitList.Clear();
            TextIconList.Clear();
            EntityList.Clear();
            Scale = new ScaleIcon(camera);

            foreach (var item in entities)
            {
                if (item.HasDataBlob<OrbitDB>() && item.GetDataBlob<OrbitDB>().Parent != null)
                {
                    if (!item.GetDataBlob<OrbitDB>().IsStationary)
                    {
                        OrbitRing ring = new OrbitRing(item, camera);
                        OrbitList.Add(ring);
                    }
                }
                if (item.HasDataBlob<NameDB>())
                {
                    TextIconList.Add(new TextIcon(item, camera));
                }


                EntityIcon entIcon = new EntityIcon(item, camera);
                EntityList.Add(entIcon);


                IconDict.Add(item.Guid, entIcon);
            }
        }

        /// <summary>
        /// Distributes the TextIcons, when they are overlapping
        /// </summary>
        public void TextIconsDistribute()
        {
            var occupiedPosition = new List<Rectangle>();
            IComparer<Rectangle> byViewPos = new ByViewPosition();

            foreach (var item in TextIconList)
            {
                item.ViewOffset = item.DefaultViewOffset;
            }

            //Consolidate TextIcons that share the same position and name
            TextIconList.Sort();
            int listLength = TextIconList.Count;
            int textIconQuantity = 1;
            for (int i = 1; i < listLength; i++)
            {
                if (TextIconList[i - 1].CompareTo(TextIconList[i]) == 0)
                {
                    textIconQuantity++;
                    TextIconList.RemoveAt(i);
                    i--;
                    listLength--;
                }
                else if (textIconQuantity > 1)
                {
                    TextIconList[i - 1].name += " x" + textIconQuantity;
                    textIconQuantity = 1;
                }
            }

            //Placement happens bottom to top, left to right
            //Each newly placed Texticon is compared to only the Texticons that are placed above its position
            //Therefore a sorted list of the occupied Positions is maintained
            occupiedPosition.Add(TextIconList[0].ViewDisplayRect);
            for (int i = 1; i < TextIconList.Count; i++)
            {
                int lowestPosIndex = occupiedPosition.BinarySearch(TextIconList[i].ViewDisplayRect + new Point(0,(int)TextIconList[i].ViewNameSize.Height) , byViewPos);
                if (lowestPosIndex < 0) lowestPosIndex = ~lowestPosIndex;
                
                for (int j = lowestPosIndex; j < occupiedPosition.Count; j++)
                {
                    if (TextIconList[i].ViewDisplayRect.Intersects(occupiedPosition[j]))
                    {
                        TextIconList[i].ViewOffset -= new PointF(0,  TextIconList[i].ViewDisplayRect.Bottom - occupiedPosition[j].Top);
                    }
                }
                //Inserts the new label sorted
                int insertIndex = occupiedPosition.BinarySearch(TextIconList[i].ViewDisplayRect, byViewPos);
                if (insertIndex < 0) insertIndex = ~insertIndex;
                occupiedPosition.Insert(insertIndex, TextIconList[i].ViewDisplayRect);
            }


        }

        /// <summary>
        /// Draws all Items in _ItemCollection
        /// In the following order:
        ///     Orbitrings
        ///     EntitySymbols
        ///     TextLabels
        /// </summary>
        /// <param name="g"></param>
        public void DrawMe(Graphics g)
        {
            foreach( var item in OrbitList)
                item.DrawMe(g);
            foreach (var item in EntityList)
                item.DrawMe(g);

            TextIconsDistribute();
            foreach (var item in TextIconList)
            {
                item.DrawMe(g);
            }

            Scale.DrawMe(g); 
        }
    }

    internal interface IIconBase
    {
        //sets the size of the icons
        float Scale { get; set; }
        void DrawMe(Graphics g);
    }


    /// <summary>
    /// IComparer for the Texticonrectangles (or any other rectancle)
    /// Sorts Bottom to top, left to right
    /// </summary>
    internal class ByViewPosition : IComparer<Rectangle>
    {
        public int Compare(Rectangle r1, Rectangle r2)
        {
            if (r1.Bottom > r2.Bottom) return -1;
            else if (r1.Bottom < r2.Bottom) return 1;
            else
            {
                if (r1.Left > r2.Left) return -1;
                else if (r1.Left < r2.Left) return 1;
                else return 0;
            }
        }
    }
}
