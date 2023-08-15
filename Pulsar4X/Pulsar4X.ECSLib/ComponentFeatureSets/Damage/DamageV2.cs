using System;
using System.Threading;

namespace Pulsar4X.ECSLib.ComponentFeatureSets.Damage
{

    public struct PointData
    {
        public byte Scale; //1 = 1m
        public byte Health; //= 0 to 1;
        public byte ComponentHealth; //amount of health this point represents of the whole component.
        public byte Fluidity; //how quickly a fluid or gass will equalise with surrounding points, 0 is solid and wont move
        public byte Temprature;
        public byte TempPhase1;
        public byte TempPhase2;
        public byte PresPhase1;
        public byte PresPhase2;
    }

    public struct DamageMap
    {
        public int Width;
        public int Height;
        public PointData[] Data;
    }

    // public class Interaction
    // {
    //     //types of projectile:
    //     //em (light/laser particles)
    //         //converts to heat.
    //         //passes through if wavelength high enough.
    //         //reflects if     
    //     //joined solid mass particles
    //     //individial mass particles
    //         //connect strength to nehbors
    //         //temprature
    //         //timed or impact or prox trigger
    //         //Material density
        
        
    //     private double heatTransfer;
        
    // }

    // public static class DamageV2
    // {

    //     public static DamageMap CreateDamageMap(ShipDesign design)
    //     {
    //         var dm = new DamageMap();
    //         double tlen = 0;
    //         double twid = 0;
    //         foreach ((ComponentDesign design, int count) component in design.Components)
    //         {
    //             var cd = component.design;
    //             var volm3 = cd.VolumePerUnit;
    //             //we convert 3d volume to 2d area
    //             var area = Math.Cbrt(volm3) * 2;
    //             var len = Math.Sqrt(area * cd.AspectRatio);
    //             var wid = area / len;
    //             tlen = len * component.count;
    //         }
            
    //         throw new NotImplementedException();
    //     }

    //     public static void DealDamage()
    //     {
    //     }


    // }
}