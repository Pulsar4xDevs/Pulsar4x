using System.Collections.Generic;
using Pulsar4X.ECSLib;
using Eto.Drawing;
using System;
using System.Linq;

namespace Pulsar4X.CrossPlatformUI.Views
{
    internal class IconCollection
    {
        //Main dictionaries for storing Icons
        //The Guid represents the entity, the Icon is created for
        public Dictionary<Guid,OrbitRing> OrbitList { get; } = new Dictionary<Guid, OrbitRing>();
        public Dictionary<Guid, TextIcon> TextIconList { get; } = new Dictionary<Guid, TextIcon>();
        public Dictionary<Guid, EntityIcon> EntityList { get; } = new Dictionary<Guid, EntityIcon>();
        public ScaleIcon Scale { get; set; }

        Camera2dv2 _camera;
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
            _camera = camera;
            Scale = new ScaleIcon(_camera);

            foreach (var item in entities)
            {
                AddNewIcon(item);
            }
        }

        internal void AddNewIcon(Entity entity)
        {
            if (entity.HasDataBlob<OrbitDB>() && entity.GetDataBlob<OrbitDB>().Parent != null)
            {
                if (!entity.GetDataBlob<OrbitDB>().IsStationary)
                {
                    OrbitRing ring = new OrbitRing(entity, _camera);
                    OrbitList.Add(entity.Guid, ring);
                }
            }
            if (entity.HasDataBlob<NameDB>())
            {
                TextIconList.Add(entity.Guid, new TextIcon(entity, _camera));
            }

            EntityIcon entIcon = new EntityIcon(entity, _camera);
            EntityList.Add(entity.Guid, entIcon);
            IconDict.Add(entity.Guid, entIcon);
        }

        internal void RemoveIcon(Entity entity)
        {
            if (OrbitList.ContainsKey(entity.Guid))
                OrbitList.Remove(entity.Guid);
            if(TextIconList.ContainsKey(entity.Guid))
                TextIconList.Remove(entity.Guid);
            if (IconDict.ContainsKey(entity.Guid))
                IconDict.Remove(entity.Guid);
        }

        internal void HandleChange(EntityChangeData changeData)
        {
            if (changeData.ChangeType == EntityChangeData.EntityChangeType.DBAdded)
            {
                if (changeData.Datablob is OrbitDB && changeData.Entity.GetDataBlob<OrbitDB>().Parent != null)
                {
                    if (!((OrbitDB)changeData.Datablob).IsStationary)
                        OrbitList[changeData.Entity.Guid] = new OrbitRing(changeData.Entity, _camera);
                }
                if (changeData.Datablob is NameDB) 
                        TextIconList[changeData.Entity.Guid] = new TextIcon(changeData.Entity, _camera);
                
                IconDict[changeData.Entity.Guid] = new EntityIcon(changeData.Entity, _camera);
            }
            if (changeData.ChangeType == EntityChangeData.EntityChangeType.DBRemoved)
            {
                if (changeData.Datablob is OrbitDB)
                    OrbitList.Remove(changeData.Entity.Guid);
                if (changeData.Datablob is NameDB)
                    TextIconList.Remove(changeData.Entity.Guid);
            }
        }

        /// <summary>
        /// Distributes the TextIcons, when they are overlapping
        /// </summary>
        public void TextIconsDistribute()
        {
            if (TextIconList.Count == 0)
                return;
            var occupiedPosition = new List<Rectangle>();
            IComparer<Rectangle> byViewPos = new ByViewPosition();
            var textIconList = new List<TextIcon>(TextIconList.Values);
            foreach (var item in TextIconList.Values)
            {
                item.ViewOffset = item.DefaultViewOffset;
            }

            //Consolidate TextIcons that share the same position and name
            textIconList.Sort();
            int listLength = textIconList.Count;
            int textIconQuantity = 1;
            for (int i = 1; i < listLength; i++)
            {
                if (textIconList[i - 1].CompareTo(textIconList[i]) == 0)
                {
                    textIconQuantity++;
                    textIconList.RemoveAt(i);
                    i--;
                    listLength--;
                }
                else if (textIconQuantity > 1)
                {
                    textIconList[i - 1].name += " x" + textIconQuantity;
                    textIconQuantity = 1;
                }
            }

            //Placement happens bottom to top, left to right
            //Each newly placed Texticon is compared to only the Texticons that are placed above its position
            //Therefore a sorted list of the occupied Positions is maintained
            occupiedPosition.Add(textIconList[0].ViewDisplayRect);

            /*
            foreach (var textIcon in TextIconList.Values)
            {
                int lowestPosIndex = occupiedPosition.BinarySearch(textIcon.ViewDisplayRect + new Point(0, (int)textIcon.ViewNameSize.Height), byViewPos);
                if (lowestPosIndex < 0) 
                    lowestPosIndex = ~lowestPosIndex;

                for (int j = lowestPosIndex; j < occupiedPosition.Count; j++)
                {
                    if (textIcon.ViewDisplayRect.Intersects(occupiedPosition[j]))
                    {
                        textIcon.ViewOffset -= new PointF(0, textIcon.ViewDisplayRect.Bottom - occupiedPosition[j].Top);
                    }
                }
                //Inserts the new label sorted
                int insertIndex = occupiedPosition.BinarySearch(textIcon.ViewDisplayRect, byViewPos);
                if (insertIndex < 0) insertIndex = ~insertIndex;
                occupiedPosition.Insert(insertIndex, textIcon.ViewDisplayRect);
            }*/


            List<TextIcon> texiconsCopy = TextIconList.Values.ToList();
            int numTextIcons = texiconsCopy.Count;

            for (int i = 1; i < numTextIcons; i++)
            {
                var item = texiconsCopy[i];
                int lowestPosIndex = occupiedPosition.BinarySearch(item.ViewDisplayRect + new Point(0,(int)item.ViewNameSize.Height) , byViewPos);
                int lpi = lowestPosIndex;
                if (lowestPosIndex < 0) 
                    lpi = ~lowestPosIndex;
                
                for (int j = lpi; j < occupiedPosition.Count; j++)
                {
                    if (item.ViewDisplayRect.Intersects(occupiedPosition[j]))
                    {
                        item.ViewOffset -= new PointF(0,  item.ViewDisplayRect.Bottom - occupiedPosition[j].Top);
                    }
                }
                //Inserts the new label sorted
                int insertIndex = occupiedPosition.BinarySearch(item.ViewDisplayRect, byViewPos);
                if (insertIndex < 0) insertIndex = ~insertIndex;
                occupiedPosition.Insert(insertIndex, item.ViewDisplayRect);
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
            foreach( var item in OrbitList.Values)
                item.DrawMe(g);
            foreach (var item in EntityList.Values)
                item.DrawMe(g);

            //TextIconsDistribute();
            foreach (var item in TextIconList.Values)
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
