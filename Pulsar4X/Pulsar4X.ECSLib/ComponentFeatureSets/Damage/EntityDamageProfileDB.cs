using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Damage
{
    public class EntityDamageProfileDB : BaseDataBlob
    {
        public (ArmorSD armorType, float thickness) Armor;
        public List<(Guid id, int count)> PlacementOrder;
        public List<(Guid, RawBmp)> TypeBitmaps;
        
        

        //public List<(int index, int size)> Bulkheads; maybe connect armor/skin at these points.
        //if we get around to doing technical stuff like being able to break a ship into two pieces,
        //and having longditudinal structural parts...
        public RawBmp DamageProfile;
        
        
        /// <summary>
        /// this allows us to encode the green value of the ShipDamageProfile to a component instance. 
        /// </summary>
        public List<ComponentInstance> ComponentLookupTable = new List<ComponentInstance>();

        [JsonConstructor]
        private EntityDamageProfileDB()
        {
        }
    
        
        public EntityDamageProfileDB(List<(ComponentDesign component, int count)> components, (ArmorSD armorSD, float thickness) armor)
        {
            List<(Guid, RawBmp)> typeBitmap = new List<(Guid, RawBmp)>();
            List<(Guid id, int count)> placementOrder = new List<(Guid, int)>();
            List<ComponentInstance> instances = new List<ComponentInstance>();
            foreach (var componenttype in components)
            {
                
                Guid typeGuid = componenttype.component.ID;
                
                RawBmp compBmp = DamageTools.CreateComponentByteArray(componenttype.component, (byte)typeBitmap.Count);
                typeBitmap.Add((typeGuid, compBmp));
                

                placementOrder.Add((typeGuid, componenttype.count));
                for (int i = 0; i < componenttype.count; i++)
                {
                    ComponentInstance newInstance = new ComponentInstance(componenttype.component);
                    instances.Add(newInstance);
                }
            }
            
            
            PlacementOrder = placementOrder;
            TypeBitmaps = typeBitmap;
            Armor = armor;
            ComponentLookupTable = instances;
            DamageProfile = ComponentPlacement.CreateShipBmp(this);
        }

        public static EntityDamageProfileDB AsteroidDamageProfile(double volume, double density, double avgRadius, int irregularity)
        {
            Random rnd = new Random(1);
            var pfl = new EntityDamageProfileDB();
            int segments = 8;
            double avgAngle = Math.PI  / segments;
            double angle = Math.PI;
            int size = (int)avgRadius * 2;
            var dmgProfile = new RawBmp(size, size);
            
            List<(int x, int y)> lineL = new List<(int x, int y)>();
            List<(int x, int y)> lineR = new List<(int x, int y)>();
            
            var startL = ((int)avgRadius, 0);
            var startR = ((int)avgRadius, 0);
            
            for (int i = 0; i < segments + 1; i++)
            {
                int jitterxL = rnd.Next(0, irregularity);
                int jitteryL = rnd.Next(-irregularity, irregularity);
                int jitterxR = rnd.Next(0, irregularity);
                int jitteryR = rnd.Next(-irregularity, irregularity);
                
                double x = avgRadius * Math.Sin(angle);
                double y = avgRadius * Math.Cos(angle);
                
                int xL = (int)(-x + avgRadius + jitteryL);
                int xR = (int)(x + avgRadius + jitteryR);
                int yL = (int)(y + avgRadius + jitterxL);
                int yR = (int)(y + avgRadius + jitterxR);
                
                BresenhamPoints(startL, (xL, yL), ref lineL);
                BresenhamPoints(startR, (xR, yR), ref lineR);

                startL = (xL, yL);
                startR = (xR, yR);
                angle -= avgAngle;

            }
            
            
            


            byte r = byte.MaxValue;
            byte g = byte.MaxValue;
            byte b = byte.MaxValue;
            byte a = byte.MaxValue;

            //fill an array with the same colour for buffer.blockcopy. 
            byte[] px = new byte[4]{r,g,b,a};
            byte[] pxarray = new byte[dmgProfile.Width * 4];
            for (int i = 0; i < dmgProfile.Width; i++)
            {
                //wonder if I can use the destination as the source, and double the amount I'm copying each time.
                Buffer.BlockCopy(px, 0, pxarray, i, 4); 
            }
            
            int height = dmgProfile.Height;
            int indexl = 0;
            int indexr = 0;
            for (int i = 0; i < height; i++)
            {
                int ypos = i;

                while (indexl < lineL.Count -1  && lineL[indexl].y == ypos)
                    indexl++;

                while (indexr < lineR.Count -1 && lineR[indexr].y == ypos)
                    indexr++;
                
                
                int leftx = lineL[indexl].x;
                int rightx = lineR[indexr].x;
                
                int width = rightx - leftx;
                
                //Buffer.BlockCopy(pxarray, 0, dmgProfile.ByteArray, leftx, width); //this should be faster, but need to debug it.
                
                
                //below is a slower but easier to write way of filling the wanted line with colour.
                for (int j = 0; j <  width; j++)
                {
                    int xpos = leftx + j;
                    dmgProfile.SetPixel(xpos, ypos, r, g, b, a); 
                    
                }
            }


            pfl.DamageProfile = dmgProfile;
            return pfl;
        }
        
        private static void BresenhamPoints((int x, int y) start,(int x, int y) end, ref List<(int x, int y)> list)
        {
            int x = start.x;
            int y = start.y;
            int x2 = end.x;
            int y2 = end.y;
            
            int w = x2 - x ;
            int h = y2 - y ;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0 ;
            if (w<0) dx1 = -1 ; else if (w>0) dx1 = 1 ;
            if (h<0) dy1 = -1 ; else if (h>0) dy1 = 1 ;
            if (w<0) dx2 = -1 ; else if (w>0) dx2 = 1 ;
            int longest = Math.Abs(w) ;
            int shortest = Math.Abs(h) ;
            if (!(longest>shortest)) 
            {
                longest = Math.Abs(h) ;
                shortest = Math.Abs(w) ;
                if (h<0) 
                    dy2 = -1 ; 
                else if (h>0) 
                    dy2 = 1 ;
                dx2 = 0 ;            
            }
            int numerator = longest >> 1 ;
            for (int i=0;i<=longest;i++) {
                list.Add((x,y));
                numerator += shortest ;
                if (!(numerator<longest)) {
                    numerator -= longest ;
                    x += dx1 ;
                    y += dy1 ;
                } else {
                    x += dx2 ;
                    y += dy2 ;
                }
            }
        }
        

        public EntityDamageProfileDB(EntityDamageProfileDB db )
        {
            Armor = db.Armor;
            PlacementOrder = db.PlacementOrder;
            TypeBitmaps = db.TypeBitmaps;
            DamageProfile = db.DamageProfile;
        }

        public override object Clone()
        {
            return new EntityDamageProfileDB(this);
        }
    }
}