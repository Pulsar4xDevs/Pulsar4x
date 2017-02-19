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
        public ScaleIcon scale { get; set; }


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
            scale = new ScaleIcon(camera);

            foreach (var item in entities)
            {
                if (item.HasDataBlob<OrbitDB>() && item.GetDataBlob<OrbitDB>().Parent != null)
                {
                    OrbitRing ring = new OrbitRing(item, camera);
                    OrbitList.Add(ring);
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
            List<Rectangle> occupiedPosition = new List<Rectangle>();
            IComparer<Rectangle> byViewPos = new ByViewPosition();

            foreach (var item in TextIconList)
            {
                item.ViewOffset = item.DefaultViewOffset;
            }

            //Consolidate TextIcons that share the same position and name
            TextIconList.Sort();
            int ListLength = TextIconList.Count;
            int TextIconQuantity = 1;
            for (int i = 1; i < ListLength; i++)
            {
                if (TextIconList[i - 1].CompareTo(TextIconList[i]) == 0)
                {
                    TextIconQuantity++;
                    TextIconList.RemoveAt(i);
                    i--;
                    ListLength--;
                }
                else if (TextIconQuantity > 1)
                {
                    TextIconList[i - 1].name += " x" + TextIconQuantity;
                    TextIconQuantity = 1;
                }
            }

            //Placement happens bottom to top, left to right
            //Each newly placed Texticon is compared to only the Texticons that are placed above its position
            //Therefore a sorted list of the occupied Positions is maintained
            occupiedPosition.Add(TextIconList[0].ViewDisplayRect);
            for (int i = 1; i < TextIconList.Count; i++)
            {
                var lowestPosIndex = occupiedPosition.BinarySearch(TextIconList[i].ViewDisplayRect + new Point(0,(int)TextIconList[i].ViewNameSize.Height) , byViewPos);
                if (lowestPosIndex < 0) lowestPosIndex = ~lowestPosIndex;
                
                for (int j = lowestPosIndex; j < occupiedPosition.Count; j++)
                {
                    if (TextIconList[i].ViewDisplayRect.Intersects(occupiedPosition[j]))
                    {
                        TextIconList[i].ViewOffset -= new PointF(0,  TextIconList[i].ViewDisplayRect.Bottom - occupiedPosition[j].Top);
                    }
                }
                //Inserts the new label sorted
                var InsertIndex = occupiedPosition.BinarySearch(TextIconList[i].ViewDisplayRect, byViewPos);
                if (InsertIndex < 0) InsertIndex = ~InsertIndex;
                occupiedPosition.Insert(InsertIndex, TextIconList[i].ViewDisplayRect);
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

            scale.DrawMe(g); 
        }
    }

    internal interface IconBase
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
