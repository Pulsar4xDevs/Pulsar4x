using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;

namespace Pulsar4X.Tests
{
    [TestFixture]
    public class shipTests
    {
        [Test]
        public void testArmor()
        {
            ShipClassTN ts2 = new ShipClassTN();
            ShipTN ts = new ShipTN();

            ts2.ShipArmorDef = new ArmorDefTN();
            ts.ShipArmor = new ArmorTN(ts2.ShipArmorDef);

            ts2.ShipArmorDef.CalcArmor(5, 38.0, 5);

            Console.WriteLine("ArmorPerHS: {0}", ts2.ShipArmorDef.armorPerHS);
            Console.WriteLine("Size: {0}", ts2.ShipArmorDef.size);
            Console.WriteLine("Cost: {0}", ts2.ShipArmorDef.cost);
            Console.WriteLine("Area: {0}", ts2.ShipArmorDef.area);
            Console.WriteLine("Depth: {0}", ts2.ShipArmorDef.depth);
            Console.WriteLine("Column Number: {0}", ts2.ShipArmorDef.cNum);

            Console.WriteLine("isDamaged: {0}", ts.ShipArmor.isDamaged);


            ts.ShipArmor.SetDamage(ts2.ShipArmorDef.cNum, ts2.ShipArmorDef.depth, 4, 1);
            for (int loop = 0; loop < ts2.ShipArmorDef.cNum; loop++)
            {
                Console.WriteLine("Column Value: {0}", ts.ShipArmor.armorColumns[loop]);
            }
            Console.WriteLine("Damage Key: {0}, Column Value: {1}", ts.ShipArmor.armorDamage.Min().Key, ts.ShipArmor.armorDamage.Min().Value);

            Console.WriteLine("isDamaged: {0}", ts.ShipArmor.isDamaged);

            ts.ShipArmor.RepairSingleBlock(ts2.ShipArmorDef.depth);

            Console.WriteLine("isDamaged: {0}", ts.ShipArmor.isDamaged);

            ts.ShipArmor.SetDamage(ts2.ShipArmorDef.cNum, ts2.ShipArmorDef.depth, 4, 1);
            for (int loop = 0; loop < ts2.ShipArmorDef.cNum; loop++)
            {
                Console.WriteLine("Column Value: {0}", ts.ShipArmor.armorColumns[loop]);
            }
            Console.WriteLine("Damage Key: {0}, Column Value: {1}", ts.ShipArmor.armorDamage.Min().Key, ts.ShipArmor.armorDamage.Min().Value);

            Console.WriteLine("isDamaged: {0}", ts.ShipArmor.isDamaged);

            ts.ShipArmor.RepairAllArmor();

            Console.WriteLine("isDamaged: {0}", ts.ShipArmor.isDamaged);

            Console.WriteLine("Cost: {0}, Area: {1},Size: {2}", ts.ShipArmor.armorDef.cost, ts.ShipArmor.armorDef.area, ts.ShipArmor.armorDef.size);
        }

        [Test]
        public void testEngine()
        {
            ShipClassTN ts2 = new ShipClassTN();
            ShipTN ts = new ShipTN();

            ts2.ShipEngineDef = new EngineDefTN("3137.6 EP Inertial Fusion Drive",32,2.65f,0.6f,0.75f,2,37,-1.0f);
            ts2.ShipEngineCount = 1;

            EngineTN temp = new EngineTN(ts2.ShipEngineDef);

            ts.ShipEngine = new BindingList<EngineTN>();
            ts.ShipEngine.Add(temp);

            EngineDefTN tst = ts.ShipEngine[0].engineDef;

            Console.WriteLine("Name: {0}", tst.name);
            Console.WriteLine("EngineBase: {0}, PowerMod: {1}, FuelConMod: {2}, ThermalReduction: {3}, Size: {4},HyperMod: {5}",
                              tst.engineBase, tst.powerMod, tst.fuelConsumptionMod, tst.thermalReduction, tst.engineSize, tst.hyperDriveMod);
            Console.WriteLine("EnginePower: {0}, FuelUsePerHour: {1}", tst.enginePower, tst.fuelUsePerHour);
            Console.WriteLine("EngineSize: {0}, EngineHTK: {1}", tst.engineSize, tst.htk);
            Console.WriteLine("ThermalSignature: {0}, ExpRisk: {1}", tst.thermalSignature, tst.expRisk);
            Console.WriteLine("IsMilitary: {0}", tst.isMilitary);
            Console.WriteLine("Crew: {0}", tst.crew);
            Console.WriteLine("Cost: {0}", tst.cost);
        }
    }



}