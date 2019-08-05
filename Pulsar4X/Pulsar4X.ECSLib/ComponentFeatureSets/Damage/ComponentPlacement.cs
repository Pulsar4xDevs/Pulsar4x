using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Constraints;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Damage
{
    public static class ComponentPlacement
    {
        
        public static EntityDamageProfileDB CreateShipMap(List<(ComponentProfile component, int count)> components, DamageResist armor)
        {

            int minWidth = 0;
            int minLength = 0;
            int maxWidth = 0;
            int maxLen = 0;


            int shipWidth = 0;
            int shipLen = 0;

            Guid maxWidthID;
            int maxWidthIndex = -1;


            EntityDamageProfileDB shipProfile = new EntityDamageProfileDB();
            
            Dictionary<Guid, int> typeCount = new Dictionary<Guid, int>();
            Dictionary<Guid, RawBmp> typeBitmap = new Dictionary<Guid, RawBmp>();

            
            List<(Guid id, int count)> placementOrder = new List<(Guid, int)>();


            Dictionary<Guid, int> UnplacedComponents = new Dictionary<Guid, int>();

            Dictionary<Guid, ComponentProfile> AllComponentsByDesign = new Dictionary<Guid, ComponentProfile>();
            foreach (var componenttype in components)
            {

                RawBmp compBmp = DamageTools.CreateComponentByteArray(componenttype.component);
                Guid typeGuid = componenttype.component.DesignGuid;

                typeCount.Add(typeGuid, componenttype.count);
                typeBitmap.Add(typeGuid, compBmp);
                UnplacedComponents.Add(typeGuid, componenttype.count);
                AllComponentsByDesign[typeGuid] = componenttype.component;
                if (minWidth < compBmp.Height)
                {
                    minWidth = compBmp.Height;
                }


                if (compBmp.Height + componenttype.count > maxWidth)
                {
                    maxWidth += compBmp.Height + componenttype.count;
                    maxWidthID = typeGuid;
                }

                maxLen += compBmp.Width * componenttype.count;
            }
            
            bool canTakeFrontConnections = true;
            bool canTakeBackConnections = true;
            foreach (var compkvp in UnplacedComponents)
            {
                ComponentProfile component = AllComponentsByDesign[compkvp.Key];
                Guid compID = compkvp.Key;
                int compCount = compkvp.Value;
                

                if (!component.Connections.HasFlag(Connections.Front)) //if we can *not* connect to front it has to go at the front.
                {
                    if (canTakeFrontConnections)
                    {
                        placementOrder.Insert(0, (compID, compCount));
                        canTakeFrontConnections = false;
                        if (compID == maxWidthID)
                            maxWidthIndex = 0;
                    }
                    else//in this case, we've got two different types of component that have to be placed at the front(currently unsuported)
                    {
                        //TODO: need to handle this properly, it could concevibly happen and maybe need an accemetric design
                        throw new Exception("Cannot Place front only component infront of a front only component");
                    }
                }
                else if (!component.Connections.HasFlag(Connections.Back)) //if the component can't connect to back 
                {
                    if (placementOrder.Count > 0 && !canTakeBackConnections) //if we've got something in the array, and it can't take connections to the back
                    {
                        //in this case, we've got two different types of component that have to be placed at the back(currently unsuported)
                        //TODO: need to handle this properly, it could concevibly happen and maybe need an accemetric design
                        throw new Exception("Cannot Place back only component behind of a back only component");
                    }
                    else
                    {
                        placementOrder.Add((compID, compCount));
                        canTakeBackConnections = false;
                        if (compID == maxWidthID)
                            maxWidthIndex = placementOrder.Count -1;
                        
                    }
                }
                else //we insert in the 'middle'. note that we're currently not handleing things that shouldnt take connections to a side.
                {
                    
                    if (canTakeBackConnections)
                        placementOrder.Add((compID, compCount));
                    else 
                        placementOrder.Insert(placementOrder.Count -1, (compID, compCount));
                }
                
            }

            shipProfile.PlacementOrder = placementOrder;
            shipProfile.TypeBitmaps = typeBitmap;
            shipProfile.Armor = armor;
            shipProfile.ShipDamageProfile = CreateShipBmp(shipProfile);
            
            return shipProfile;
        }

        public static RawBmp CreateShipBmp(EntityDamageProfileDB shipProfile)
        {
            var armorID = shipProfile.Armor.IDCode;
            var po = shipProfile.PlacementOrder;
            Dictionary<Guid, RawBmp> typeBitmaps = shipProfile.TypeBitmaps;
            int componentWidthNum = 0;

            int totalLen = 0;
            var totalWidth = 0;
            int widestPoint = 0;
            int widestLen = 0;
            foreach (var componentkvp in po)
            {
                var typeid = componentkvp.id;
                var count = componentkvp.count;
                var typeBmp = typeBitmaps[typeid];
                if (count > componentWidthNum)
                    componentWidthNum = count;

                totalLen += typeBmp.Width;

                int width = typeBmp.Height * count;
                if (width > totalWidth)
                {
                    totalWidth = width;
                    widestPoint = totalLen;
                    widestLen = typeBmp.Width;
                }
            }

            int canvasWidth = totalWidth + 6; //create a bit larger canvas size for the armor.
            int canvasLen = totalLen + 6;


            int size = 4 * canvasLen * canvasWidth;
            int stride = canvasLen * 4;
            RawBmp shipBmp = new RawBmp()
            {
                ByteArray = new byte[size],
                Stride = stride,
                Depth =  4,
                Width = canvasLen,
                Height = canvasWidth,
            };
            byte[] shipByteArray = new byte[size];

            int offsetx = 4;
            foreach (var componentkvp in po)
            {
                
                var typeid = componentkvp.id;
                var count = componentkvp.count;
                var typeBmp = typeBitmaps[typeid];

                int pixHeight = typeBmp.Height * count;

                int offsety = (canvasWidth - pixHeight) / 2;

                int bytesPerLine = 4 * typeBmp.Width;

                for (int i = 0; i < count; i++)
                {
                    for (int pxstrip = 0; pxstrip < typeBmp.Height; pxstrip++)
                    {
                        int srcpos = typeBmp.Stride * pxstrip;
                        int destPos = shipBmp.GetOffset(offsetx, offsety + pxstrip + (typeBmp.Height * i));

                        Buffer.BlockCopy(typeBmp.ByteArray, srcpos, shipByteArray, destPos, bytesPerLine);
                    }
                }

                offsetx += typeBmp.Width;

            }

            shipBmp.ByteArray = shipByteArray;

            
            //create bezzier control points for front and rear armor/skin
            
            (int x, int y)[] controlPointsFore = new (int x, int y)[4];

            controlPointsFore[0] = (0, canvasWidth / 2);
            controlPointsFore[1] = (0, 2);
            controlPointsFore[2] = (0, 2);
            controlPointsFore[3] = (widestPoint - widestLen, 2);
            
            (int x, int y)[] controlPointsAft = new (int x, int y)[4];
            int lastbmpHeight = typeBitmaps[po[po.Count -1].id].Height * po[po.Count -1].count / 2;
            int lastbmpLen = typeBitmaps[po[po.Count -1].id].Width ;
            
            controlPointsAft[0] = (widestPoint + 3 , 2); //add three for the extra canvas size
            controlPointsAft[1] = (totalLen - lastbmpLen, 2 + lastbmpHeight);
            controlPointsAft[2] = (totalLen - lastbmpLen, 2 + lastbmpHeight);
            controlPointsAft[3] = (canvasLen, canvasWidth / 2 -  lastbmpHeight);
            
            List<(int x, int y)> linePoints = new List<(int x, int y)>();
            float dt = 0.01f;
            for (float t = 0.0f; t < 1.0; t += dt)
            {
                var x = BezCalc(t, controlPointsFore[0].x, controlPointsFore[1].x, controlPointsFore[2].x, controlPointsFore[3].x);
                var y = BezCalc(t, controlPointsFore[0].y, controlPointsFore[1].y, controlPointsFore[2].y, controlPointsFore[3].y);
                
                linePoints.Add(((int)x, (int)y));
                
            }
            
            for (float t = 0.0f; t < 1.0; t += dt)
            {
                var x = BezCalc(t, controlPointsAft[0].x, controlPointsAft[1].x, controlPointsAft[2].x, controlPointsAft[3].x);
                var y = BezCalc(t, controlPointsAft[0].y, controlPointsAft[1].y, controlPointsAft[2].y, controlPointsAft[3].y);
                
                linePoints.Add(((int)x, (int)y));
                
            }
            
            //set the pixels in teh bitmap for the bezier curves.
            
            foreach (var coord in linePoints)
            {
                shipBmp.SetPixel(coord.x, coord.y, armorID, 200, 200, 255);
                shipBmp.SetPixel(coord.x, canvasWidth -  coord.y, armorID, 200, 200, 255);
            }
            
            //connect the front and rear armor/skin.
            //TODO: handle angles, currently expects start and end y to be the same.
            (int x, int y) straightStart = controlPointsFore[3];
            (int x, int y) straightEnd = controlPointsAft[0];
            int straightArmorLen = straightEnd.x - straightStart.x;
            for (int i = 0; i < straightArmorLen; i++)
            {
                shipBmp.SetPixel(straightStart.x + i, straightStart.y, armorID, 200, 200, 255);
                shipBmp.SetPixel(straightStart.x + i, canvasWidth - straightStart.y, armorID, 200, 200, 255);
            }

            
            shipProfile.ShipDamageProfile = shipBmp;
            return shipBmp;
        }
        
        private static double BezCalc(double t, double a0, double a1, double a2, double a3)
        {
            double foo = a0 * Math.Pow((1 - t), 3); 
            foo += a1 * 3 * t * Math.Pow((1 - t), 2); 
            foo += a2 * 3 * Math.Pow(t, 2) * (1 - t); 
            foo += a3 * Math.Pow(t, 3);
            return foo;
        }



    }
}