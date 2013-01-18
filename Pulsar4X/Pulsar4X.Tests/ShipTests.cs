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
            ShipTN ts = new ShipTN(ts2,0,0);

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
            ShipTN ts = new ShipTN(ts2,0,0);

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
            ShipTN ts = new ShipTN(ts2,0,0);

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
            ShipTN ts = new ShipTN(ts2,0,0);

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
            PassiveSensorDefTN EMPasDef = new PassiveSensorDefTN("EM Sensor EM1-5", 1.0f, 5, PassiveSensorType.EM, 1.0f, 1);

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

            TestClass.AddCrewQuarters(CrewQ,-1);

            Console.WriteLine("Size: {0}, Crew: {1}, Cost: {2}, HTK: {3}, Tonnage: {4}", TestClass.SizeHS, TestClass.TotalRequiredCrew, TestClass.BuildPointCost, TestClass.TotalHTK, TestClass.SizeTons);

            Console.WriteLine("HS Accomodations/Required: {0}/{1}, Total Fuel Capacity: {2}, Total MSP: {3}, Engineering percentage: {4}, Has Bridge: {5}, Total Required Crew: {6}", TestClass.AccomHSAvailable, TestClass.AccomHSRequirement,
            TestClass.TotalFuelCapacity, TestClass.TotalMSPCapacity, (TestClass.SizeHS / TestClass.EngineeringHS), TestClass.HasBridge, TestClass.TotalRequiredCrew);

            Console.WriteLine("Armor Size: {0}, Cost: {1}", TestClass.ShipArmorDef.size, TestClass.ShipArmorDef.cost);

            Console.WriteLine("Ship Engine Power: {0}, Ship Thermal Signature: {1}, Ship Fuel Use Per Hour: {2}", TestClass.MaxEnginePower, TestClass.MaxThermalSignature, TestClass.MaxFuelUsePerHour);

            Console.WriteLine("Best TH: {0}, BestEM: {1}, Max EM Signature: {2}, Total Cross Section: {3}", TestClass.BestThermalRating, TestClass.BestEMRating, TestClass.MaxEMSignature, TestClass.TotalCrossSection);

            TestClass.AddCrewQuarters(CrewQ, -1);

            Console.WriteLine("Size: {0}, Crew: {1}, Cost: {2}, HTK: {3}, Tonnage: {4}", TestClass.SizeHS, TestClass.TotalRequiredCrew, TestClass.BuildPointCost, TestClass.TotalHTK, TestClass.SizeTons);

            Console.WriteLine("HS Accomodations/Required: {0}/{1}, Total Fuel Capacity: {2}, Total MSP: {3}, Engineering percentage: {4}, Has Bridge: {5}, Total Required Crew: {6}", TestClass.AccomHSAvailable, TestClass.AccomHSRequirement,
            TestClass.TotalFuelCapacity, TestClass.TotalMSPCapacity, (TestClass.SizeHS / TestClass.EngineeringHS), TestClass.HasBridge, TestClass.TotalRequiredCrew);

            Console.WriteLine("Armor Size: {0}, Cost: {1}", TestClass.ShipArmorDef.size, TestClass.ShipArmorDef.cost);

            Console.WriteLine("Ship Engine Power: {0}, Ship Thermal Signature: {1}, Ship Fuel Use Per Hour: {2}", TestClass.MaxEnginePower, TestClass.MaxThermalSignature, TestClass.MaxFuelUsePerHour);

            Console.WriteLine("Best TH: {0}, BestEM: {1}, Max EM Signature: {2}, Total Cross Section: {3}", TestClass.BestThermalRating, TestClass.BestEMRating, TestClass.MaxEMSignature, TestClass.TotalCrossSection);


            TestClass.AddCrewQuarters(CrewQ, 2);


            ShipTN testShip = new ShipTN(TestClass,0,0);

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

        [Test]
        public void TGActiveSortThermalSortTest()
        {
            EngineDefTN EngDef = new EngineDefTN("25 EP Nuclear Thermal Engine", 5, 1.0f, 1.0f, 1.0f, 1, 5, -1.0f);

            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, GeneralType.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, GeneralType.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, GeneralType.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, GeneralType.Bridge);

            Faction FID = new Faction(0);
            StarSystem System = new StarSystem();
            Planet planet = new Planet();


            TaskGroupTN TaskGroup1 = new TaskGroupTN("Taskforce 001", FID, planet,System);

            for (int loop = 0; loop < 5; loop++)
            {
                ShipClassTN test = new ShipClassTN("Ship");
                test.AddCrewQuarters(CrewQ, 2);
                test.AddFuelStorage(FuelT, 2);
                test.AddEngineeringSpaces(EBay, 2);
                test.AddOtherComponent(Bridge, 1);

                int add = 0;
                switch(loop)
                {
                    case 0 : add = 2; break;
                    case 1 : add = 4; break;
                    case 2 : add = 1; break;
                    case 3 : add = 5; break;
                    case 4 : add = 3; break;
                }
                test.AddEngine(EngDef, (byte)add);

                Console.WriteLine("Speed:{0}", test.MaxSpeed);

                TaskGroup1.AddShip(test,0);
                Console.WriteLine("{0} {1}", TaskGroup1, TaskGroup1.Ships[loop].ShipsTaskGroup);
            }

            LinkedListNode<int> AS = TaskGroup1.ActiveSortList.First;
            LinkedListNode<int> TS = TaskGroup1.ThermalSortList.First;
            for(int loop = 0; loop < 5; loop++)
            {
                Console.WriteLine("AL:{0},TL:{1} || Ship{2} AL:{3},TL:{4} : {5} {6} {7} {8} {9} {10} {11}", AS.Value, TS.Value, loop,TaskGroup1.Ships[loop].ActiveList.Value, TaskGroup1.Ships[loop].ThermalList.Value, 
                    TaskGroup1.Ships[loop].CurrentSpeed,TaskGroup1.Ships[loop].CurrentEnginePower,TaskGroup1.Ships[loop].CurrentThermalSignature, TaskGroup1.Ships[loop].ShipClass.MaxEnginePower,
                    TaskGroup1.Ships[loop].ShipClass.MaxThermalSignature,TaskGroup1.Ships[loop].CurrentFuelUsePerHour,TaskGroup1.Ships[loop].ShipClass.MaxFuelUsePerHour);

                AS = AS.Next;
                TS = TS.Next;
            }
        }

        [Test]
        public void TGPassivesTest()
        {
            EngineDefTN EngDef = new EngineDefTN("25 EP Nuclear Thermal Engine", 5, 1.0f, 1.0f, 1.0f, 1, 5, -1.0f);
            PassiveSensorDefTN ThPasDef1 = new PassiveSensorDefTN("Thermal Sensor TH1-5", 1.0f, 5, PassiveSensorType.Thermal, 1.0f, 1);
            PassiveSensorDefTN ThPasDef2 = new PassiveSensorDefTN("Thermal Sensor TH1-6", 1.0f, 6, PassiveSensorType.Thermal, 1.0f, 1);
            PassiveSensorDefTN ThPasDef3 = new PassiveSensorDefTN("Thermal Sensor TH1-8", 1.0f, 8, PassiveSensorType.Thermal, 1.0f, 1);
            PassiveSensorDefTN ThPasDef4 = new PassiveSensorDefTN("Thermal Sensor TH1-11", 1.0f, 11, PassiveSensorType.Thermal, 1.0f, 1);


            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, GeneralType.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, GeneralType.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, GeneralType.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, GeneralType.Bridge);

            Faction FID = new Faction(0);
            StarSystem System = new StarSystem();
            Planet planet = new Planet();

            TaskGroupTN TaskGroup1 = new TaskGroupTN("Taskforce 001", FID, planet,System);
            for (int loop = 0; loop < 4; loop++)
            {


                ShipClassTN test = new ShipClassTN("Ship");
                test.AddCrewQuarters(CrewQ, 2);
                test.AddFuelStorage(FuelT, 2);
                test.AddEngineeringSpaces(EBay, 2);
                test.AddOtherComponent(Bridge, 1);

                switch (loop)
                {
                    case 0: test.AddPassiveSensor(ThPasDef2, 5);
                    break;
                    case 1: test.AddPassiveSensor(ThPasDef1, 4);
                    break;
                    case 2: test.AddPassiveSensor(ThPasDef3, 7);
                    break;
                    case 3: test.AddPassiveSensor(ThPasDef4, 6);
                    break;
                }

                TaskGroup1.AddShip(test,0);
                Console.WriteLine("Best Thermal:{0},{1}", TaskGroup1.BestThermal.pSensorDef.rating, TaskGroup1.BestThermalCount);
            }
        }

        [Test]
        public void TGActiveTest()
        {
            EngineDefTN EngDef = new EngineDefTN("25 EP Nuclear Thermal Engine", 5, 1.0f, 1.0f, 1.0f, 1, 5, -1.0f);
            ActiveSensorDefTN ActDef1 = new ActiveSensorDefTN("Search 5M - 5000", 1.0f, 10, 5, 100, false, 1.0f, 1);
            ActiveSensorDefTN ActDef2 = new ActiveSensorDefTN("Search 500k - 1", 1.0f, 10, 5, 1, false, 1.0f, 1);
            ActiveSensorDefTN ActDef3 = new ActiveSensorDefTN("Search 2.2M - 1000", 1.0f, 10, 5, 20, false, 1.0f, 1);
            ActiveSensorDefTN ActDef4 = new ActiveSensorDefTN("Search 7M - 10000", 1.0f, 10, 5, 200, false, 1.0f, 1);

            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, GeneralType.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, GeneralType.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, GeneralType.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, GeneralType.Bridge);


            Faction FID = new Faction(0);
            StarSystem System = new StarSystem();
            Planet planet = new Planet();

            TaskGroupTN TaskGroup1 = new TaskGroupTN("Taskforce 001", FID, planet,System);
            for (int loop = 0; loop < 4; loop++)
            {


                ShipClassTN test = new ShipClassTN("Ship");
                test.AddCrewQuarters(CrewQ, 2);
                test.AddFuelStorage(FuelT, 2);
                test.AddEngineeringSpaces(EBay, 2);
                Console.WriteLine("Bridge isn't present: {0} {1}", test.OtherComponents.IndexOf(Bridge), test.HasBridge);
                test.AddOtherComponent(Bridge, 1);
                Console.WriteLine("Bridge is present: {0} {1}", test.OtherComponents.IndexOf(Bridge),test.HasBridge);

                switch (loop)
                {
                    case 0: test.AddActiveSensor(ActDef2, 2);
                        break;
                    case 1: test.AddActiveSensor(ActDef1, 2);
                        break;
                    case 2: test.AddActiveSensor(ActDef3, 2);
                        break;
                    case 3: test.AddActiveSensor(ActDef4, 2);
                        break;
                }

                TaskGroup1.AddShip(test,0);

                TaskGroup1.SetActiveSensor(loop, 0, true);
                TaskGroup1.SetActiveSensor(loop, 1, true);
            }

            LinkedListNode<int> EM = TaskGroup1.EMSortList.First;
            for (int loop = 0; loop < 4; loop++)
            {
                Console.WriteLine("{0} {1}", TaskGroup1.Ships[loop].CurrentEMSignature, EM.Value);
                EM = EM.Next;
            }

            for (int loop = 0; loop < Constants.ShipTN.ResolutionMax; loop++)
            {
                Console.WriteLine("{0} | {1}", TaskGroup1.TaskGroupLookUpST[loop],loop);
            }

            TaskGroup1.SetActiveSensor(2, 0, false);
            TaskGroup1.SetActiveSensor(2, 1, false);

            EM = TaskGroup1.EMSortList.First;
            for (int loop = 0; loop < 4; loop++)
            {
                Console.WriteLine("{0} {1}", TaskGroup1.Ships[loop].CurrentEMSignature, EM.Value);
                EM = EM.Next;
            }

            for (int loop = 0; loop < Constants.ShipTN.ResolutionMax; loop++)
            {
                Console.WriteLine("{0} | {1}", TaskGroup1.TaskGroupLookUpST[loop], loop);
            }
        }

        //Issue/Follow Orders Test
        [Test]
        public void TGOrdersTest()
        {
            EngineDefTN EngDef = new EngineDefTN("25 EP Nuclear Thermal Engine", 5, 1.0f, 1.0f, 1.0f, 1, 5, -1.0f);

            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, GeneralType.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, GeneralType.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, GeneralType.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, GeneralType.Bridge);


            Faction FID = new Faction(0);
            StarSystem System = new StarSystem();
            Planet planet = new Planet();


            Waypoint WP1 = new Waypoint(System,0.1,0.1);

            planet.XSystem = 0.0;
            planet.YSystem = 0.0;


            TaskGroupTN TaskGroup1 = new TaskGroupTN("Taskforce 001", FID, planet, System);

            ShipClassTN test = new ShipClassTN("Ship");
            test.AddCrewQuarters(CrewQ, 2);
            test.AddFuelStorage(FuelT, 2);
            test.AddEngineeringSpaces(EBay, 2);
            test.AddOtherComponent(Bridge, 1);
            test.AddEngine(EngDef, 1);

            TaskGroup1.AddShip(test,0);

            TaskGroup1.Ships[0].Refuel(200000.0f);

            Orders TGOrder = new Orders(Constants.ShipTN.OrderType.MoveTo, -1, -1, WP1);

            TaskGroup1.IssueOrder(TGOrder);

            Console.WriteLine("Fuel Remaining:{0}", TaskGroup1.Ships[0].CurrentFuel);

            while (TaskGroup1.TaskGroupOrders.Count != 0)
            {
                TaskGroup1.FollowOrders(5);
                Console.WriteLine("{0} {1} | {2} {3}", TaskGroup1.Contact.SystemKmX, TaskGroup1.Contact.SystemKmY, TaskGroup1.Contact.XSystem, TaskGroup1.Contact.YSystem);
            }

            Console.WriteLine("Fuel Remaining:{0}", TaskGroup1.Ships[0].CurrentFuel);
        }


        [Test]
        public void FactionSystemTest()
        {
            Faction PlayerFaction1 = new Faction(0);
            Faction PlayerFaction2 = new Faction(1);

            StarSystem System1 = new StarSystem("Sol");
            StarSystem System2 = new StarSystem("Alpha Centauri");

            Waypoint Start1 = new Waypoint(System1,1.0, 1.0);
            Waypoint Start2 = new Waypoint(System1,1.0005, 1.0005);



            PlayerFaction1.AddNewShipDesign("Blucher");
            PlayerFaction2.AddNewShipDesign("Tribal");

            PlayerFaction1.ShipDesigns[0].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[0].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[0].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 2);
            PlayerFaction1.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction1.ShipDesigns[0].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[0].AddActiveSensor(PlayerFaction1.ComponentList.ActiveSensorDef[0], 1);
            PlayerFaction1.ShipDesigns[0].AddPassiveSensor(PlayerFaction1.ComponentList.PassiveSensorDef[0], 1);
            PlayerFaction1.ShipDesigns[0].AddPassiveSensor(PlayerFaction1.ComponentList.PassiveSensorDef[1], 1);

            PlayerFaction2.ShipDesigns[0].AddEngine(PlayerFaction2.ComponentList.Engines[0], 1);
            PlayerFaction2.ShipDesigns[0].AddCrewQuarters(PlayerFaction2.ComponentList.CrewQuarters[0], 2);
            PlayerFaction2.ShipDesigns[0].AddFuelStorage(PlayerFaction2.ComponentList.FuelStorage[0], 2);
            PlayerFaction2.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction2.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction2.ShipDesigns[0].AddOtherComponent(PlayerFaction2.ComponentList.OtherComponents[0], 1);
            PlayerFaction2.ShipDesigns[0].AddActiveSensor(PlayerFaction2.ComponentList.ActiveSensorDef[0], 1);
            PlayerFaction2.ShipDesigns[0].AddPassiveSensor(PlayerFaction2.ComponentList.PassiveSensorDef[0], 1);
            PlayerFaction2.ShipDesigns[0].AddPassiveSensor(PlayerFaction2.ComponentList.PassiveSensorDef[1], 1);

            PlayerFaction1.AddNewTaskGroup("P1 TG 01", Start1, System1);
            PlayerFaction2.AddNewTaskGroup("P2 TG 01", Start2, System1);

            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[0],0);
            PlayerFaction2.TaskGroups[0].AddShip(PlayerFaction2.ShipDesigns[0],0);


            PlayerFaction1.AddNewContactList(System1);
            PlayerFaction2.AddNewContactList(System1);

            PlayerFaction1.AddNewContactList(System2);
            PlayerFaction2.AddNewContactList(System2);

            PlayerFaction1.TaskGroups[0].SetActiveSensor(0, 0, true);
            PlayerFaction2.TaskGroups[0].SetActiveSensor(0, 0, true);

            Console.WriteLine("Time: 0 {0} {1}", PlayerFaction1.TaskGroups[0].Contact.CurrentSystem.Name, PlayerFaction2.TaskGroups[0].Contact.CurrentSystem.Name);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].ThermalDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].ThermalDetection[0]);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].EMDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].EMDetection[0]);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].ActiveDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].ActiveDetection[0]);

            PlayerFaction1.SensorSweep(5);
            PlayerFaction2.SensorSweep(5);

            Console.WriteLine("Time: 5 {0} {1}", PlayerFaction1.TaskGroups[0].Contact.CurrentSystem.Name, PlayerFaction2.TaskGroups[0].Contact.CurrentSystem.Name);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].ThermalDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].ThermalDetection[0]);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].EMDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].EMDetection[0]);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].ActiveDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].ActiveDetection[0]);

            PlayerFaction1.TaskGroups[0].SetActiveSensor(0, 0, false);
            PlayerFaction2.TaskGroups[0].SetActiveSensor(0, 0, false);

            PlayerFaction1.SensorSweep(10);
            PlayerFaction2.SensorSweep(10);

            Console.WriteLine("Time: 10 {0} {1}", PlayerFaction1.TaskGroups[0].Contact.CurrentSystem.Name, PlayerFaction2.TaskGroups[0].Contact.CurrentSystem.Name);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].ThermalDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].ThermalDetection[0]);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].EMDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].EMDetection[0]);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].ActiveDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].ActiveDetection[0]);

            PlayerFaction1.TaskGroups[0].SetActiveSensor(0, 0, true);
            PlayerFaction2.TaskGroups[0].SetActiveSensor(0, 0, true);

            System1.AddJumpPoint(0.1, 0.2);
            System2.AddJumpPoint(0.2, 0.1);

            System1.JumpPoints[0].Connect = System2.JumpPoints[0];
            System2.JumpPoints[0].Connect = System1.JumpPoints[0];

            System1.JumpPoints[0].StandardTransit(PlayerFaction1.TaskGroups[0]);
            System1.JumpPoints[0].StandardTransit(PlayerFaction2.TaskGroups[0]);

            PlayerFaction1.SensorSweep(15);
            PlayerFaction2.SensorSweep(15);

            Console.WriteLine("Time: 15 {0} {1}", PlayerFaction1.TaskGroups[0].Contact.CurrentSystem.Name, PlayerFaction2.TaskGroups[0].Contact.CurrentSystem.Name);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].ThermalDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].ThermalDetection[0]);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].EMDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].EMDetection[0]);
            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].ActiveDetection[1], PlayerFaction2.TaskGroups[0].Ships[0].ActiveDetection[0]);
        }

        [Test]
        public void CargoLoadUnloadTest()
        {
            Faction PlayerFaction1 = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet();
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);

            System1.Stars[0].Planets[0].XSystem = 1.0;
            System1.Stars[0].Planets[0].YSystem = 1.0;


            PlayerFaction1.AddNewShipDesign("Blucher");

            PlayerFaction1.ShipDesigns[0].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[0].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[0].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 2);
            PlayerFaction1.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction1.ShipDesigns[0].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[0].AddCargoHold(PlayerFaction1.ComponentList.CargoHoldDef[1], 1);

            PlayerFaction1.AddNewTaskGroup("P1 TG 01", System1.Stars[0].Planets[0], System1);

            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[0], 0);

            Population P1 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);
            Population P2 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);

            System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number = 3.0f;
            System1.Stars[0].Planets[0].Populations[1].Installations[(int)Installation.InstallationType.Infrastructure].Number = 0.0f;

            Console.WriteLine("Infrastructure on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number,
                System1.Stars[0].Planets[0].Populations[1].Installations[(int)Installation.InstallationType.Infrastructure].Number);

            PlayerFaction1.TaskGroups[0].LoadCargo(System1.Stars[0].Planets[0].Populations[0], Installation.InstallationType.Infrastructure, 1);

            Console.WriteLine("Infrastructure on cargo tg after load in tons:{0}", PlayerFaction1.TaskGroups[0].CargoList[Installation.InstallationType.Infrastructure].tons);

            PlayerFaction1.TaskGroups[0].UnloadCargo(System1.Stars[0].Planets[0].Populations[1], Installation.InstallationType.Infrastructure, 1);

            Console.WriteLine("Infrastructure on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number,
    System1.Stars[0].Planets[0].Populations[1].Installations[(int)Installation.InstallationType.Infrastructure].Number);

            Console.WriteLine("Infrastructure on cargo tg after unload :{0}", PlayerFaction1.TaskGroups[0].CargoList[Installation.InstallationType.Infrastructure].tons);
        }

        [Test]
        public void CargoOrdersTest()
        {
            Faction PlayerFaction1 = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet();
            Planet pl2 = new Planet();
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);
            System1.Stars[0].Planets.Add(pl2);

            System1.Stars[0].Planets[0].XSystem = 1.0;
            System1.Stars[0].Planets[0].YSystem = 1.0;

            System1.Stars[0].Planets[1].XSystem = 2.0;
            System1.Stars[0].Planets[1].YSystem = 2.0;


            PlayerFaction1.AddNewShipDesign("Blucher");

            PlayerFaction1.ShipDesigns[0].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[0].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[0].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 2);
            PlayerFaction1.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction1.ShipDesigns[0].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[0].AddCargoHold(PlayerFaction1.ComponentList.CargoHoldDef[1], 1);
            PlayerFaction1.ShipDesigns[0].AddCargoHandlingSystem(PlayerFaction1.ComponentList.CargoHandleSystemDef[0], 1);

            PlayerFaction1.AddNewTaskGroup("P1 TG 01", System1.Stars[0].Planets[0], System1);

            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[0], 0);

            Population P1 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);
            Population P2 = new Population(System1.Stars[0].Planets[1], PlayerFaction1);

            System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number = 3.0f;
            System1.Stars[0].Planets[1].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number = 0.0f;

            Orders Load = new Orders(Constants.ShipTN.OrderType.LoadInstallation, (int)Installation.InstallationType.Infrastructure, 1, System1.Stars[0].Planets[0].Populations[0]);
            Orders Unload = new Orders(Constants.ShipTN.OrderType.UnloadInstallation, (int)Installation.InstallationType.Infrastructure, 1, System1.Stars[0].Planets[1].Populations[0]);

            PlayerFaction1.TaskGroups[0].IssueOrder(Load);
            PlayerFaction1.TaskGroups[0].IssueOrder(Unload);


            Console.WriteLine("Infrastructure on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number,
                System1.Stars[0].Planets[1].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number);


            while (PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count > 0)
            {
                Console.WriteLine("Current Order Time: {0} {1}", PlayerFaction1.TaskGroups[0].TimeRequirement,
    PlayerFaction1.TaskGroups[0].TaskGroupOrders[0].orderTimeRequirement);

                PlayerFaction1.TaskGroups[0].FollowOrders(Constants.TimeInSeconds.ThirtyMinutes);

                Console.WriteLine("Order Count: {0}", PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count);
            }

            Console.WriteLine("Infrastructure on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number,
    System1.Stars[0].Planets[1].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number);

            Console.WriteLine("Infrastructure on cargo tg after unload :{0}", PlayerFaction1.TaskGroups[0].CargoList[Installation.InstallationType.Infrastructure].tons);
        }


        [Test]
        public void ColonyOrdersTest()
        {
            Faction PlayerFaction1 = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet();
            Planet pl2 = new Planet();
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);
            System1.Stars[0].Planets.Add(pl2);

            System1.Stars[0].Planets[0].XSystem = 1.0;
            System1.Stars[0].Planets[0].YSystem = 1.0;

            System1.Stars[0].Planets[1].XSystem = 2.0;
            System1.Stars[0].Planets[1].YSystem = 2.0;


            PlayerFaction1.AddNewShipDesign("Blucher");

            PlayerFaction1.ShipDesigns[0].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[0].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[0].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 2);
            PlayerFaction1.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction1.ShipDesigns[0].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[0].AddColonyBay(PlayerFaction1.ComponentList.ColonyBayDef[0], 1);
            PlayerFaction1.ShipDesigns[0].AddCargoHandlingSystem(PlayerFaction1.ComponentList.CargoHandleSystemDef[0], 1);

            PlayerFaction1.AddNewTaskGroup("P1 TG 01", System1.Stars[0].Planets[0], System1);

            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[0], 0);

            Population P1 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);
            Population P2 = new Population(System1.Stars[0].Planets[1], PlayerFaction1);

            System1.Stars[0].Planets[0].Populations[0].CivilianPopulation = 5.0f;
            System1.Stars[0].Planets[1].Populations[0].CivilianPopulation = 1.0f;

            Orders Load = new Orders(Constants.ShipTN.OrderType.LoadColonists, 9000, -1, System1.Stars[0].Planets[0].Populations[0]);
            Orders Unload = new Orders(Constants.ShipTN.OrderType.UnloadColonists, 9000, -1, System1.Stars[0].Planets[1].Populations[0]);

            PlayerFaction1.TaskGroups[0].IssueOrder(Load);
            PlayerFaction1.TaskGroups[0].IssueOrder(Unload);

            while (PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count > 0)
            {
                Console.WriteLine("Current Order Time: {0} {1}", PlayerFaction1.TaskGroups[0].TimeRequirement,
    PlayerFaction1.TaskGroups[0].TaskGroupOrders[0].orderTimeRequirement);

                PlayerFaction1.TaskGroups[0].FollowOrders(Constants.TimeInSeconds.ThirtyMinutes);

                Console.WriteLine("Order Count: {0}", PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count);
            }

            Console.WriteLine("Population on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].CivilianPopulation,
    System1.Stars[0].Planets[1].Populations[0].CivilianPopulation);
        }
    }
}