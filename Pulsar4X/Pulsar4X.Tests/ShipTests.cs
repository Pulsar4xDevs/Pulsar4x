using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class shipTests
    {
        [Test]
        public void testArmor()
        {
            ShipClass ts2 = new ShipClass();
            Ship ts = new Ship();

            ts2.ShipArmorDef = new ArmorDef();
            ts.ShipArmor = new Armor();

            ts2.ShipArmorDef.CalcArmor(5, 38.0, 5);

            Console.WriteLine("ArmorPerHS: {0}", ts2.ShipArmorDef.ArmorPerHS);
            Console.WriteLine("Size: {0}", ts2.ShipArmorDef.Size);
            Console.WriteLine("Cost: {0}", ts2.ShipArmorDef.Cost);
            Console.WriteLine("Area: {0}", ts2.ShipArmorDef.Area);
            Console.WriteLine("Depth: {0}", ts2.ShipArmorDef.Depth);
            Console.WriteLine("Column Number: {0}", ts2.ShipArmorDef.CNum);

            Console.WriteLine("isDamaged: {0}", ts.ShipArmor.isDamaged);


            ts.ShipArmor.SetDamage(ts2.ShipArmorDef.CNum, ts2.ShipArmorDef.Depth, 4, 1);
            for (int loop = 0; loop < ts2.ShipArmorDef.CNum; loop++)
            {
                Console.WriteLine("Column Value: {0}", ts.ShipArmor.armorColumns[loop]);
            }
            Console.WriteLine("Damage Key: {0}, Column Value: {1}", ts.ShipArmor.armorDamage.Min().Key, ts.ShipArmor.armorDamage.Min().Value);

            Console.WriteLine("isDamaged: {0}", ts.ShipArmor.isDamaged);

            ts.ShipArmor.RepairSingleBlock(ts2.ShipArmorDef.Depth);

            Console.WriteLine("isDamaged: {0}", ts.ShipArmor.isDamaged);

            ts.ShipArmor.SetDamage(ts2.ShipArmorDef.CNum, ts2.ShipArmorDef.Depth, 4, 1);
            for (int loop = 0; loop < ts2.ShipArmorDef.CNum; loop++)
            {
                Console.WriteLine("Column Value: {0}", ts.ShipArmor.armorColumns[loop]);
            }
            Console.WriteLine("Damage Key: {0}, Column Value: {1}", ts.ShipArmor.armorDamage.Min().Key, ts.ShipArmor.armorDamage.Min().Value);

            Console.WriteLine("isDamaged: {0}", ts.ShipArmor.isDamaged);

            ts.ShipArmor.RepairAllArmor();

            Console.WriteLine("isDamaged: {0}", ts.ShipArmor.isDamaged);



        }
    }



}