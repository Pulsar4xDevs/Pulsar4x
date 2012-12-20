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
            ShipClassTN ts2 = new ShipClassTN("Test");
            ShipTN ts = new ShipTN(ts2);

            ts2.ShipArmorDef = new ArmorDefTN("Duranium Armour");
            ts.ShipArmor = new ArmorTN(ts2.ShipArmorDef);

            ts2.ShipArmorDef.CalcArmor("Duranium Armor",5, 38.0, 5);

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
        public void testArmorNA()
        {
            ArmorDefNA ArmorTestDefNA = new ArmorDefNA("High Density Duranium",80);
            ArmorTestDefNA.CalcArmor(5918, 3);

            ArmorNA ArmorTestNA = new ArmorNA(ArmorTestDefNA);

            Console.WriteLine("Size: {0}", ArmorTestDefNA.unitMass);
            Console.WriteLine("Cost: {0}", ArmorTestDefNA.cost);
            Console.WriteLine("Area: {0}", ArmorTestDefNA.area);
            Console.WriteLine("Depth: {0}", ArmorTestDefNA.depth);
            Console.WriteLine("Column Number: {0}", ArmorTestDefNA.columnNumber);

            Console.WriteLine("isDamaged: {0}", ArmorTestNA.isDamaged);

            ArmorTestNA.SetDamage(ArmorTestDefNA.columnNumber, ArmorTestDefNA.depth, 4, 1);
            for (int loop = 0; loop < ArmorTestDefNA.columnNumber; loop++)
            {
                Console.WriteLine("Column Value: {0}", ArmorTestNA.armorColumns[loop]);
            }
            Console.WriteLine("Damage Key: {0}, Column Value: {1}", ArmorTestNA.armorDamage.Min().Key, ArmorTestNA.armorDamage.Min().Value);

            Console.WriteLine("isDamaged: {0}", ArmorTestNA.isDamaged);

            ArmorTestNA.RepairSingleBlock(ArmorTestDefNA.depth);

            Console.WriteLine("isDamaged: {0}", ArmorTestNA.isDamaged);

            ArmorTestNA.SetDamage(ArmorTestDefNA.columnNumber, ArmorTestDefNA.depth, 4, 1);
            for (int loop = 0; loop < ArmorTestDefNA.columnNumber; loop++)
            {
                Console.WriteLine("Column Value: {0}", ArmorTestNA.armorColumns[loop]);
            }
            Console.WriteLine("Damage Key: {0}, Column Value: {1}", ArmorTestNA.armorDamage.Min().Key, ArmorTestNA.armorDamage.Min().Value);

            Console.WriteLine("isDamaged: {0}", ArmorTestNA.isDamaged);

            ArmorTestNA.RepairAllArmor();

            Console.WriteLine("isDamaged: {0}", ArmorTestNA.isDamaged);

            Console.WriteLine("Cost: {0}, Area: {1},Size: {2}", ArmorTestNA.armorDef.cost, ArmorTestNA.armorDef.area, ArmorTestNA.armorDef.unitMass);
        }

        [Test]
        public void testEngine()
        {
            ShipClassTN ts2 = new ShipClassTN("Test");
            ShipTN ts = new ShipTN(ts2);

            ts2.ShipEngineDef = new EngineDefTN("3137.6 EP Inertial Fusion Drive",32,2.65f,0.6f,0.75f,2,37,-1.0f);
            ts2.ShipEngineCount = 1;

            EngineTN temp = new EngineTN(ts2.ShipEngineDef);

            ts.ShipEngine = new BindingList<EngineTN>();
            ts.ShipEngine.Add(temp);

            EngineDefTN tst = ts.ShipEngine[0].engineDef;

            Console.WriteLine("Name: {0}", tst.name);
            Console.WriteLine("EngineBase: {0}, PowerMod: {1}, FuelConMod: {2}, ThermalReduction: {3}, Size: {4},HyperMod: {5}",
                              tst.engineBase, tst.powerMod, tst.fuelConsumptionMod, tst.thermalReduction, tst.size, tst.hyperDriveMod);
            Console.WriteLine("EnginePower: {0}, FuelUsePerHour: {1}", tst.enginePower, tst.fuelUsePerHour);
            Console.WriteLine("EngineSize: {0}, EngineHTK: {1}", tst.size, tst.htk);
            Console.WriteLine("ThermalSignature: {0}, ExpRisk: {1}", tst.thermalSignature, tst.expRisk);
            Console.WriteLine("IsMilitary: {0}", tst.isMilitary);
            Console.WriteLine("Crew: {0}", tst.crew);
            Console.WriteLine("Cost: {0}", tst.cost);
        }

        [Test]
        public void testPSensor()
        {
            ShipClassTN ts2 = new ShipClassTN("Test");
            ShipTN ts = new ShipTN(ts2);

            PassiveSensorDefTN PSensorDefTest = new PassiveSensorDefTN("Thermal Sensor TH19-342", 19.0f, 18, PassiveSensorType.Thermal, 1.0f, 1);

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
            ShipClassTN ts2 = new ShipClassTN("Test");
            ShipTN ts = new ShipTN(ts2);

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

        [Test]
        public void testShip()
        {
            /// <summary>
            /// These would go into a faction component list I think
            /// </summary>
            EngineDefTN EngDef = new EngineDefTN("25 EP Nuclear Thermal Engine", 5, 1.0f, 1.0f, 1.0f, 1, 5, -1.0f);
            ActiveSensorDefTN ActDef = new ActiveSensorDefTN("Search 5M - 5000", 1.0f, 10, 5, 100, false, 1.0f, 1);
            PassiveSensorDefTN ThPasDef = new PassiveSensorDefTN("Thermal Sensor TH1-5", 1.0f, 5, PassiveSensorType.Thermal, 1.0f, 1);
            PassiveSensorDefTN EMPasDef = new PassiveSensorDefTN("Thermal Sensor TH1-5", 1.0f, 5, PassiveSensorType.EM, 1.0f, 1);

            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, GeneralType.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, GeneralType.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, GeneralType.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, GeneralType.Bridge);

            ShipClassTN TestClass = new ShipClassTN("Test Ship Class");

            TestClass.AddCrewQuarters(CrewQ, 2);
            TestClass.AddFuelStorage(FuelT, 2);
            TestClass.AddEngineeringSpaces(EBay, 2);
            TestClass.AddOtherComponent(Bridge, 1);

            TestClass.AddEngine(EngDef, 1);

            TestClass.AddPassiveSensor(ThPasDef, 1);
            TestClass.AddPassiveSensor(EMPasDef, 1);

            TestClass.AddActiveSensor(ActDef, 1);

            Console.WriteLine("Size: {0}, Crew: {1}, Cost: {2}, HTK: {3}, Tonnage: {4}", TestClass.SizeHS, TestClass.TotalRequiredCrew, TestClass.BuildPointCost, TestClass.TotalHTK, TestClass.SizeTons);

            Console.WriteLine("HS Accomodations/Required: {0}/{1}, Total Fuel Capacity: {2}, Total MSP: {3}, Engineering percentage: {4}, Has Bridge: {5}, Total Required Crew: {6}", TestClass.AccomHSAvailable, TestClass.AccomHSRequirement,
            TestClass.TotalFuelCapacity,TestClass.TotalMSPCapacity, (TestClass.SizeHS / TestClass.EngineeringHS), TestClass.HasBridge, TestClass.TotalRequiredCrew);
            
            Console.WriteLine("Armor Size: {0}, Cost: {1}", TestClass.ShipArmorDef.size, TestClass.ShipArmorDef.cost);

            Console.WriteLine("Ship Engine Power: {0}, Ship Thermal Signature: {1}, Ship Fuel Use Per Hour: {2}", TestClass.MaxEnginePower, TestClass.MaxThermalSignature, TestClass.MaxFuelUsePerHour);

            Console.WriteLine("Best TH: {0}, BestEM: {1}, Max EM Signature: {2}, Total Cross Section: {3}", TestClass.BestThermalRating, TestClass.BestEMRating, TestClass.MaxEMSignature, TestClass.TotalCrossSection);



            ShipTN testShip = new ShipTN(TestClass);

            testShip.CrewQuarters[0].isDestroyed = true;

            for (int loop = 0; loop < testShip.CrewQuarters.Count; loop++)
            {
                Console.WriteLine("Crew Quarters {0} isDestroyed:{1}", loop + 1, testShip.CrewQuarters[loop].isDestroyed);
            }

            testShip.CrewQuarters[0].isDestroyed = false;

            Console.WriteLine("Engine Power/Fuel Usage/Thermal Signature/Speed: {0}/{1}/{2}/{3}", testShip.CurrentEnginePower, testShip.CurrentFuelUsePerHour, testShip.CurrentThermalSignature,
                testShip.CurrentSpeed);

            testShip.SetSpeed(1000);

            Console.WriteLine("Engine Power/Fuel Usage/Thermal Signature/Speed: {0}/{1}/{2}/{3}", testShip.CurrentEnginePower, testShip.CurrentFuelUsePerHour, testShip.CurrentThermalSignature,
                testShip.CurrentSpeed);

            Console.WriteLine("Current Crew/Fuel/MSP: {0}/{1}/{2}", testShip.CurrentCrew, testShip.CurrentFuel, testShip.CurrentMSP);

            int CrewSource = 100000;
            float FuelSource = 100000.0f;
            int MSPSource = 100000;

            CrewSource = testShip.Recrew(CrewSource);
            FuelSource = testShip.Refuel(FuelSource);
            MSPSource = testShip.Resupply(MSPSource);

            Console.WriteLine("Current Crew/Fuel/MSP: {0}/{1}/{2} Source: {3}/{4}/{5}", testShip.CurrentCrew, testShip.CurrentFuel, testShip.CurrentMSP, CrewSource,FuelSource,MSPSource);

            Console.WriteLine("Current EM Signature: {0}", testShip.CurrentEMSignature);


            bool isActive = true;
            testShip.SetSensor(testShip.ShipASensor[0], isActive);

            Console.WriteLine("Current EM Signature: {0}", testShip.CurrentEMSignature);

            isActive = false;
            testShip.SetSpeed(1500);
            testShip.SetSensor(testShip.ShipASensor[0], isActive);

            Console.WriteLine("Engine Power/Fuel Usage/Thermal Signature/Speed: {0}/{1}/{2}/{3}", testShip.CurrentEnginePower, testShip.CurrentFuelUsePerHour, testShip.CurrentThermalSignature,
    testShip.CurrentSpeed);
            Console.WriteLine("Current EM Signature: {0}", testShip.CurrentEMSignature);


        }
    }



}