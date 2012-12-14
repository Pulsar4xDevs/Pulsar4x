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

        [Test]
        public void testPSensor()
        {
            ShipClassTN ts2 = new ShipClassTN();
            ShipTN ts = new ShipTN();

            PassiveSensorDefTN PSensorDefTest = new PassiveSensorDefTN("Thermal Sensor TH19-342", 19.0f, 18, false, 1.0f, 1);

            ts2.ShipPSensorDef = new BindingList<PassiveSensorDefTN>();
            ts2.ShipPSensorCount = new BindingList<ushort>();
            ts2.ShipPSensorDef.Add(PSensorDefTest);
            ts2.ShipPSensorCount.Add(1);

            PassiveSensorTN PSensorTest = new PassiveSensorTN(ts2.ShipPSensorDef[0]);

            ts.ShipPSensor = new BindingList<PassiveSensorTN>();
            ts.ShipPSensor.Add(PSensorTest);


            PassiveSensorDefTN tst3 = ts.ShipPSensor[0].pSensorDef;

            Console.WriteLine("Name: {0}", tst3.name);
            Console.WriteLine("Size: {0}, HTK: {1}, Hardening: {2}", tst3.size, tst3.htk, tst3.hardening);
            Console.WriteLine("Rating: {0}, Range: {1}", tst3.rating, tst3.range);
            Console.WriteLine("IsMilitary: {0}", tst3.isMilitary);
            Console.WriteLine("Crew: {0}", tst3.crew);
            Console.WriteLine("Cost: {0}", tst3.cost);

            for (ushort loop = 80; loop < 120; loop++)
            {
                Console.WriteLine("Signature:{0} Detection Range in KM:{1}", loop, tst3.GetPassiveDetectionRange(loop));
            }
        }

        [Test]
        public void testASensor()
        {
            ShipClassTN ts2 = new ShipClassTN();
            ShipTN ts = new ShipTN();

            ActiveSensorDefTN ASensorDefTest = new ActiveSensorDefTN("Active Search Sensor MR705-R185", 6.0f, 36,24,185, false, 1.0f, 1);

            ts2.ShipASensorDef = new BindingList<ActiveSensorDefTN>();
            ts2.ShipASensorCount = new BindingList<ushort>();
            ts2.ShipASensorDef.Add(ASensorDefTest);
            ts2.ShipASensorCount.Add(1);

            ActiveSensorTN ASensorTest = new ActiveSensorTN(ts2.ShipASensorDef[0]);

            ts.ShipASensor = new BindingList<ActiveSensorTN>();
            ts.ShipASensor.Add(ASensorTest);


            ActiveSensorDefTN tst3 = ts.ShipASensor[0].aSensorDef;

            Console.WriteLine("Name: {0}", tst3.name);
            Console.WriteLine("Size: {0}, HTK: {1}, Hardening: {2}", tst3.size, tst3.htk, tst3.hardening);
            Console.WriteLine("GPS: {0}, Range: {1}", tst3.gps, tst3.maxRange);
            Console.WriteLine("IsMilitary: {0}", tst3.isMilitary);
            Console.WriteLine("Crew: {0}", tst3.crew);
            Console.WriteLine("Cost: {0}", tst3.cost);

            for (ushort loop = 80; loop < 120; loop++)
            {
                Console.WriteLine("Resolution:{0} Detection Range in KM:{1}", loop, tst3.GetActiveDetectionRange(loop,-1));
            }
        }
    }



}