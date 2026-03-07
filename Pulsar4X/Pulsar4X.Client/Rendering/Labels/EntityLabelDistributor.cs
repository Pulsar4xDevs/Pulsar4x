using Pulsar4X.Engine;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;

namespace Pulsar4X.Client
{
    public delegate IEnumerable<EntityLabel> SystemLabelDistributor(IEnumerable<EntityLabel> labels);

    public static class EntityLabelDistributor
    {
        public static IEnumerable<EntityLabel> Noop(IEnumerable<EntityLabel> labels) => labels;

        public static IEnumerable<EntityLabel> Group(IEnumerable<EntityLabel> labels)
        {
            List<List<EntityLabel>> nameIconGroupings = new List<List<EntityLabel>>();
            bool[] alreadyGroupedItems = Enumerable.Repeat(false, labels.Count()).ToArray();

            int iterations = 0;
            foreach(var nameIcon in labels)
            {
                if(!alreadyGroupedItems[iterations])
                {
                    nameIconGroupings.Add(new List<EntityLabel>());
                    nameIconGroupings[nameIconGroupings.Count -1].Add(nameIcon);
                    alreadyGroupedItems[iterations] = true;
                    int nestedIterations = 0;
                    foreach(var nestedNameIcon in labels)
                    {
                        if(iterations != nestedIterations && !alreadyGroupedItems[nestedIterations])
                        {
                            //check if two names are within the same pixel of distance, if so groups them together into a single window to prevent name overlapping.
                            var xDistance = Helpers.GetSingleDistanceSquared(nameIcon.Rect.X, nestedNameIcon.Rect.X);
                            var yDistance = Helpers.GetSingleDistanceSquared(nameIcon.Rect.Y, nestedNameIcon.Rect.Y);

                            if(yDistance < 256 && xDistance < 9216)
                            {
                                nameIconGroupings[nameIconGroupings.Count -1].Add(nestedNameIcon);
                                alreadyGroupedItems[nestedIterations] = true;
                            }
                        }
                        nestedIterations++;
                    }
                }
                iterations++;
            }

            var icons = new List<EntityLabel>();

            // FIXME: this feels inefficient
            foreach(var nameIconGrouping in nameIconGroupings)
            {
                var grp = nameIconGrouping
                    .GroupBy(x => Utils.EntityBodyType(x.Entity))
                    .OrderBy(x => x.Key);
                var first = (EntityLabelExtCombo)grp.First().Take(1).First();

                // FIXME: eww
                var s = new HashSet<Entity>();
                for (int i = 0; i < grp.Count(); i++)
                {
                    var itm = grp.ElementAt(i);
                    if (i == 0)
                    {
                        var o = itm.Skip(1);
                        foreach (var j in o)
                            s.Add(j.Entity);
                    }
                    else
                    {
                        foreach (var j in itm)
                            s.Add(j.Entity);
                    }
                }
                first.SetEntities(s);

                icons.Add(first);
            }

            return icons;
        }

        public static IEnumerable<EntityLabel> Spiral(IEnumerable<EntityLabel> labels)
        {
            var occupiedPosition = new List<RectangleF>();

            // Step values on failed placement
            float angleStep = MathF.PI / 8;
            float radiusStep = 2;

            foreach (var item in labels)
            {
                // Starting angle and search radius
                float angle = 0;
                float radius = 0;

                // How many times to try placement
                var tries = 100;

                // Original location
                var origLocation = item.Rect.Location;

                bool intersects;
                do
                {
                    var size = new SizeF(
                            radius * MathF.Cos(angle),
                            radius * MathF.Sin(angle));
                    var opos = item.Rect.Location;
                    item.Rect.Location = PointF.Add(opos, size);

                    intersects = false;

                    foreach (var o in occupiedPosition)
                    {
                        if (item.Rect.IntersectsWith(o))
                        {
                            // Label is intersecting. Try a different position
                            radius += radiusStep;
                            angle += angleStep;
                            intersects = true;
                            tries -= 1;
                            break;
                        }
                    }
                }
                while (tries > 0 && intersects);

                if (intersects)
                {
                    // We are still intersecting. Use the original location
                    item.Rect.Location = origLocation;
                }

                occupiedPosition.Add(item.Rect);
            }

            return labels;
        }
    }
}
