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
            Faction newFaction = new Faction(0);
            ShipClassTN ts2 = new ShipClassTN("Test", newFaction);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);

            TaskGroupTN newTG = new TaskGroupTN("TG", newFaction, System1.Stars[0].Planets[0], System1);
            ShipTN ts = new ShipTN(ts2, 0, 0, newTG, newFaction);

            ts2.ShipArmorDef = new ArmorDefTN("Duranium Armour");
            ts.ShipArmor = new ArmorTN(ts2.ShipArmorDef);

            ts2.ShipArmorDef.CalcArmor("Duranium Armor", 5, 38.0, 5);

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
            ArmorDefNA ArmorTestDefNA = new ArmorDefNA("High Density Duranium", 80);
            ArmorTestDefNA.CalcArmor(5918, 3);

            ArmorNA ArmorTestNA = new ArmorNA(ArmorTestDefNA);

            Console.WriteLine("Size: {0}", ArmorTestDefNA.m_oUnitMass);
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

            Console.WriteLine("Cost: {0}, Area: {1},Size: {2}", ArmorTestNA.armorDef.cost, ArmorTestNA.armorDef.area, ArmorTestNA.armorDef.m_oUnitMass);
        }

        [Test]
        public void testEngine()
        {

            Faction newFaction = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);

            TaskGroupTN newTG = new TaskGroupTN("TG", newFaction, System1.Stars[0].Planets[0], System1);

            ShipClassTN ts2 = new ShipClassTN("Test", newFaction);
            ShipTN ts = new ShipTN(ts2, 0, 0, newTG, newFaction);

            ts2.ShipEngineDef = new EngineDefTN("3137.6 EP Inertial Fusion Drive", 32, 2.65f, 0.6f, 0.75f, 2, 37, -1.0f);
            ts2.ShipEngineCount = 1;

            EngineTN temp = new EngineTN(ts2.ShipEngineDef);

            ts.ShipEngine = new BindingList<EngineTN>();
            ts.ShipEngine.Add(temp);

            EngineDefTN tst = ts.ShipEngine[0].engineDef;

            Console.WriteLine("Name: {0}", tst.Name);
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

            Faction newFaction = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);

            TaskGroupTN newTG = new TaskGroupTN("TG", newFaction, System1.Stars[0].Planets[0], System1);

            ShipClassTN ts2 = new ShipClassTN("Test", newFaction);
            ShipTN ts = new ShipTN(ts2, 0, 0, newTG, newFaction);

            PassiveSensorDefTN PSensorDefTest = new PassiveSensorDefTN("Thermal Sensor TH19-342", 19.0f, 18, PassiveSensorType.Thermal, 1.0f, 1);

            ts2.ShipPSensorDef = new BindingList<PassiveSensorDefTN>();
            ts2.ShipPSensorCount = new BindingList<ushort>();
            ts2.ShipPSensorDef.Add(PSensorDefTest);
            ts2.ShipPSensorCount.Add(1);

            PassiveSensorTN PSensorTest = new PassiveSensorTN(ts2.ShipPSensorDef[0]);

            ts.ShipPSensor = new BindingList<PassiveSensorTN>();
            ts.ShipPSensor.Add(PSensorTest);


            PassiveSensorDefTN tst3 = ts.ShipPSensor[0].pSensorDef;

            Console.WriteLine("Name: {0}", tst3.Name);
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

            Faction newFaction = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);

            TaskGroupTN newTG = new TaskGroupTN("TG", newFaction, System1.Stars[0].Planets[0], System1);

            ShipClassTN ts2 = new ShipClassTN("Test", newFaction);
            ShipTN ts = new ShipTN(ts2, 0, 0, newTG, newFaction);

            ActiveSensorDefTN ASensorDefTest = new ActiveSensorDefTN("Active Search Sensor MR705-R185", 6.0f, 36, 24, 185, false, 1.0f, 1);

            ts2.ShipASensorDef = new BindingList<ActiveSensorDefTN>();
            ts2.ShipASensorCount = new BindingList<ushort>();
            ts2.ShipASensorDef.Add(ASensorDefTest);
            ts2.ShipASensorCount.Add(1);

            ActiveSensorTN ASensorTest = new ActiveSensorTN(ts2.ShipASensorDef[0]);

            ts.ShipASensor = new BindingList<ActiveSensorTN>();
            ts.ShipASensor.Add(ASensorTest);


            ActiveSensorDefTN tst3 = ts.ShipASensor[0].aSensorDef;

            Console.WriteLine("Name: {0}", tst3.Name);
            Console.WriteLine("Size: {0}, HTK: {1}, Hardening: {2}", tst3.size, tst3.htk, tst3.hardening);
            Console.WriteLine("GPS: {0}, Range: {1}", tst3.gps, tst3.maxRange);
            Console.WriteLine("IsMilitary: {0}", tst3.isMilitary);
            Console.WriteLine("Crew: {0}", tst3.crew);
            Console.WriteLine("Cost: {0}", tst3.cost);

            for (ushort loop = 80; loop < 120; loop++)
            {
                Console.WriteLine("Resolution:{0} Detection Range in KM:{1}", loop, tst3.GetActiveDetectionRange(loop, -1));
            }
        }

        [Test]
        public void testShip()
        {
            Faction newFaction = new Faction(0);

            /// <summary>
            /// These would go into a faction component list I think
            /// </summary>
            EngineDefTN EngDef = new EngineDefTN("25 EP Nuclear Thermal Engine", 5, 1.0f, 1.0f, 1.0f, 1, 5, -1.0f);
            ActiveSensorDefTN ActDef = new ActiveSensorDefTN("Search 5M - 5000", 1.0f, 10, 5, 100, false, 1.0f, 1);
            PassiveSensorDefTN ThPasDef = new PassiveSensorDefTN("Thermal Sensor TH1-5", 1.0f, 5, PassiveSensorType.Thermal, 1.0f, 1);
            PassiveSensorDefTN EMPasDef = new PassiveSensorDefTN("EM Sensor EM1-5", 1.0f, 5, PassiveSensorType.EM, 1.0f, 1);

            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, ComponentTypeTN.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, ComponentTypeTN.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, ComponentTypeTN.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, ComponentTypeTN.Bridge);

            ShipClassTN TestClass = new ShipClassTN("Test Ship Class", newFaction);

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
            TestClass.TotalFuelCapacity, TestClass.TotalMSPCapacity, (TestClass.EngineeringHS / TestClass.SizeHS), TestClass.HasBridge, TestClass.TotalRequiredCrew);

            Console.WriteLine("Armor Size: {0}, Cost: {1}", TestClass.ShipArmorDef.size, TestClass.ShipArmorDef.cost);

            Console.WriteLine("Ship Engine Power: {0}, Ship Thermal Signature: {1}, Ship Fuel Use Per Hour: {2}", TestClass.MaxEnginePower, TestClass.MaxThermalSignature, TestClass.MaxFuelUsePerHour);

            Console.WriteLine("Best TH: {0}, BestEM: {1}, Max EM Signature: {2}, Total Cross Section: {3}", TestClass.BestThermalRating, TestClass.BestEMRating, TestClass.MaxEMSignature, TestClass.TotalCrossSection);

            TestClass.AddCrewQuarters(CrewQ, -1);

            Console.WriteLine("Size: {0}, Crew: {1}, Cost: {2}, HTK: {3}, Tonnage: {4}", TestClass.SizeHS, TestClass.TotalRequiredCrew, TestClass.BuildPointCost, TestClass.TotalHTK, TestClass.SizeTons);

            Console.WriteLine("HS Accomodations/Required: {0}/{1}, Total Fuel Capacity: {2}, Total MSP: {3}, Engineering percentage: {4}, Has Bridge: {5}, Total Required Crew: {6}", TestClass.AccomHSAvailable, TestClass.AccomHSRequirement,
            TestClass.TotalFuelCapacity, TestClass.TotalMSPCapacity, (TestClass.EngineeringHS / TestClass.SizeHS), TestClass.HasBridge, TestClass.TotalRequiredCrew);

            Console.WriteLine("Armor Size: {0}, Cost: {1}", TestClass.ShipArmorDef.size, TestClass.ShipArmorDef.cost);

            Console.WriteLine("Ship Engine Power: {0}, Ship Thermal Signature: {1}, Ship Fuel Use Per Hour: {2}", TestClass.MaxEnginePower, TestClass.MaxThermalSignature, TestClass.MaxFuelUsePerHour);

            Console.WriteLine("Best TH: {0}, BestEM: {1}, Max EM Signature: {2}, Total Cross Section: {3}", TestClass.BestThermalRating, TestClass.BestEMRating, TestClass.MaxEMSignature, TestClass.TotalCrossSection);

            TestClass.AddCrewQuarters(CrewQ, -1);

            Console.WriteLine("Size: {0}, Crew: {1}, Cost: {2}, HTK: {3}, Tonnage: {4}", TestClass.SizeHS, TestClass.TotalRequiredCrew, TestClass.BuildPointCost, TestClass.TotalHTK, TestClass.SizeTons);

            Console.WriteLine("HS Accomodations/Required: {0}/{1}, Total Fuel Capacity: {2}, Total MSP: {3}, Engineering percentage: {4}, Has Bridge: {5}, Total Required Crew: {6}", TestClass.AccomHSAvailable, TestClass.AccomHSRequirement,
            TestClass.TotalFuelCapacity, TestClass.TotalMSPCapacity, (TestClass.EngineeringHS / TestClass.SizeHS), TestClass.HasBridge, TestClass.TotalRequiredCrew);

            Console.WriteLine("Armor Size: {0}, Cost: {1}", TestClass.ShipArmorDef.size, TestClass.ShipArmorDef.cost);

            Console.WriteLine("Ship Engine Power: {0}, Ship Thermal Signature: {1}, Ship Fuel Use Per Hour: {2}", TestClass.MaxEnginePower, TestClass.MaxThermalSignature, TestClass.MaxFuelUsePerHour);

            Console.WriteLine("Best TH: {0}, BestEM: {1}, Max EM Signature: {2}, Total Cross Section: {3}", TestClass.BestThermalRating, TestClass.BestEMRating, TestClass.MaxEMSignature, TestClass.TotalCrossSection);


            TestClass.AddCrewQuarters(CrewQ, 2);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);

            TaskGroupTN newTG = new TaskGroupTN("TG", newFaction, System1.Stars[0].Planets[0], System1);


            ShipTN testShip = new ShipTN(TestClass, 0, 0, newTG, newFaction);

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

            Console.WriteLine("Current Crew/Fuel/MSP: {0}/{1}/{2} Source: {3}/{4}/{5}", testShip.CurrentCrew, testShip.CurrentFuel, testShip.CurrentMSP, CrewSource, FuelSource, MSPSource);

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

            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, ComponentTypeTN.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, ComponentTypeTN.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, ComponentTypeTN.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, ComponentTypeTN.Bridge);

            Faction FID = new Faction(0);
            StarSystem System = new StarSystem();
            Star S1 = new Star();
            Planet planet = new Planet(S1, S1);


            TaskGroupTN TaskGroup1 = new TaskGroupTN("Taskforce 001", FID, planet, System);

            for (int loop = 0; loop < 5; loop++)
            {
                ShipClassTN test = new ShipClassTN("Ship", FID);
                test.AddCrewQuarters(CrewQ, 2);
                test.AddFuelStorage(FuelT, 2);
                test.AddEngineeringSpaces(EBay, 2);
                test.AddOtherComponent(Bridge, 1);

                int add = 0;
                switch (loop)
                {
                    case 0: add = 2; break;
                    case 1: add = 4; break;
                    case 2: add = 1; break;
                    case 3: add = 5; break;
                    case 4: add = 3; break;
                }
                test.AddEngine(EngDef, (byte)add);

                Console.WriteLine("Speed:{0}", test.MaxSpeed);

                TaskGroup1.AddShip(test, 0);
                Console.WriteLine("{0} {1}", TaskGroup1, TaskGroup1.Ships[loop].ShipsTaskGroup);
            }

            LinkedListNode<int> AS = TaskGroup1.ActiveSortList.First;
            LinkedListNode<int> ES = TaskGroup1.EMSortList.First;
            LinkedListNode<int> TS = TaskGroup1.ThermalSortList.First;
            for (int loop = 0; loop < 5; loop++)
            {
                Console.Write("AL:{0}, EL:{1}, TL:{2} || Ship{3} AL:{4}, EL:{5} TL:{6} |||", AS.Value, ES.Value, TS.Value, loop, TaskGroup1.Ships[loop].ActiveList.Value,
                                                                                             TaskGroup1.Ships[loop].EMList.Value, TaskGroup1.Ships[loop].ThermalList.Value);


                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6}", TaskGroup1.Ships[loop].CurrentSpeed, TaskGroup1.Ships[loop].CurrentEnginePower, TaskGroup1.Ships[loop].CurrentThermalSignature, TaskGroup1.Ships[loop].ShipClass.MaxEnginePower,
                    TaskGroup1.Ships[loop].ShipClass.MaxThermalSignature, TaskGroup1.Ships[loop].CurrentFuelUsePerHour, TaskGroup1.Ships[loop].ShipClass.MaxFuelUsePerHour);

                AS = AS.Next;
                ES = ES.Next;
                TS = TS.Next;
            }

            TaskGroup1.Ships[4].CurrentThermalSignature = 500;
            TaskGroup1.SortShipBySignature(TaskGroup1.Ships[4].ThermalList, TaskGroup1.ThermalSortList, 0);

            Console.WriteLine("------------------------");

            AS = TaskGroup1.ActiveSortList.First;
            ES = TaskGroup1.EMSortList.First;
            TS = TaskGroup1.ThermalSortList.First;
            for (int loop = 0; loop < 5; loop++)
            {
                Console.Write("AL:{0}, EL:{1}, TL:{2} || Ship{3} AL:{4}, EL:{5} TL:{6} |||", AS.Value, ES.Value, TS.Value, loop, TaskGroup1.Ships[loop].ActiveList.Value,
                                                                                             TaskGroup1.Ships[loop].EMList.Value, TaskGroup1.Ships[loop].ThermalList.Value);


                Console.WriteLine("{0} {1} {2} {3} {4} {5} {6}", TaskGroup1.Ships[loop].CurrentSpeed, TaskGroup1.Ships[loop].CurrentEnginePower, TaskGroup1.Ships[loop].CurrentThermalSignature, TaskGroup1.Ships[loop].ShipClass.MaxEnginePower,
                    TaskGroup1.Ships[loop].ShipClass.MaxThermalSignature, TaskGroup1.Ships[loop].CurrentFuelUsePerHour, TaskGroup1.Ships[loop].ShipClass.MaxFuelUsePerHour);

                AS = AS.Next;
                ES = ES.Next;
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


            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, ComponentTypeTN.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, ComponentTypeTN.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, ComponentTypeTN.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, ComponentTypeTN.Bridge);

            Faction FID = new Faction(0);
            StarSystem System = new StarSystem();
            Star S1 = new Star();
            Planet planet = new Planet(S1, S1);

            TaskGroupTN TaskGroup1 = new TaskGroupTN("Taskforce 001", FID, planet, System);
            for (int loop = 0; loop < 4; loop++)
            {


                ShipClassTN test = new ShipClassTN("Ship", FID);
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

                TaskGroup1.AddShip(test, 0);
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

            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, ComponentTypeTN.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, ComponentTypeTN.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, ComponentTypeTN.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, ComponentTypeTN.Bridge);


            Faction FID = new Faction(0);
            StarSystem System = new StarSystem();
            Star S1 = new Star();
            Planet planet = new Planet(S1, S1);

            TaskGroupTN TaskGroup1 = new TaskGroupTN("Taskforce 001", FID, planet, System);
            for (int loop = 0; loop < 4; loop++)
            {


                ShipClassTN test = new ShipClassTN("Ship", FID);
                test.AddCrewQuarters(CrewQ, 2);
                test.AddFuelStorage(FuelT, 2);
                test.AddEngineeringSpaces(EBay, 2);
                Console.WriteLine("Bridge isn't present: {0} {1}", test.OtherComponents.IndexOf(Bridge), test.HasBridge);
                test.AddOtherComponent(Bridge, 1);
                Console.WriteLine("Bridge is present: {0} {1}", test.OtherComponents.IndexOf(Bridge), test.HasBridge);

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

                TaskGroup1.AddShip(test, 0);

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
                Console.WriteLine("{0} | {1}", TaskGroup1.TaskGroupLookUpST[loop], loop);
            }

            TaskGroup1.SetActiveSensor(2, 0, false);
            TaskGroup1.SetActiveSensor(2, 1, false);

            Console.WriteLine("--------------------------------------------");

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

            GeneralComponentDefTN CrewQ = new GeneralComponentDefTN("Crew Quarters", 1.0f, 0, 10.0m, ComponentTypeTN.Crew);
            GeneralComponentDefTN FuelT = new GeneralComponentDefTN("Fuel Storage", 1.0f, 0, 10.0m, ComponentTypeTN.Fuel);
            GeneralComponentDefTN EBay = new GeneralComponentDefTN("Engineering Spaces", 1.0f, 5, 10.0m, ComponentTypeTN.Engineering);
            GeneralComponentDefTN Bridge = new GeneralComponentDefTN("Bridge", 1.0f, 5, 10.0m, ComponentTypeTN.Bridge);


            Faction FID = new Faction(0);
            StarSystem System = new StarSystem();
            Star S1 = new Star();
            Planet planet = new Planet(S1, S1);


            Waypoint WP1 = new Waypoint("WP TG Orders", System, 0.1, 0.1, 0);

            planet.XSystem = 0.0;
            planet.YSystem = 0.0;


            TaskGroupTN TaskGroup1 = new TaskGroupTN("Taskforce 001", FID, planet, System);

            ShipClassTN test = new ShipClassTN("Ship", FID);
            test.AddCrewQuarters(CrewQ, 2);
            test.AddFuelStorage(FuelT, 2);
            test.AddEngineeringSpaces(EBay, 2);
            test.AddOtherComponent(Bridge, 1);
            test.AddEngine(EngDef, 1);

            TaskGroup1.AddShip(test, 0);

            TaskGroup1.Ships[0].Refuel(200000.0f);

            Orders TGOrder = new Orders(Constants.ShipTN.OrderType.MoveTo, -1, -1, 0, WP1);

            TaskGroup1.IssueOrder(TGOrder);

            Console.WriteLine("Fuel Remaining:{0}", TaskGroup1.Ships[0].CurrentFuel);

            while (TaskGroup1.TaskGroupOrders.Count != 0)
            {
                TaskGroup1.FollowOrders(5);
                Console.WriteLine("{0} {1} | {2} {3}", TaskGroup1.Contact.XSystem * Constants.Units.KM_PER_AU, TaskGroup1.Contact.YSystem * Constants.Units.KM_PER_AU, TaskGroup1.Contact.XSystem, TaskGroup1.Contact.YSystem);
            }

            Console.WriteLine("Fuel Remaining:{0}", TaskGroup1.Ships[0].CurrentFuel);
        }


        [Test]
        public void FactionSystemTest()
        {
            Faction PlayerFaction1 = new Faction(0);
            Faction PlayerFaction2 = new Faction(1);

            StarSystem System1 = new StarSystem("Sol");
            Star S1 = new Star();
            System1.Stars.Add(S1);
            StarSystem System2 = new StarSystem("Alpha Centauri");
            Star S2 = new Star();
            System2.Stars.Add(S2);

            Planet Start1 = new Planet(System1.Stars[0], System1.Stars[0]);
            Start1.XSystem = 1.0;
            Start1.YSystem = 1.0;


            Planet Start2 = new Planet(System2.Stars[0], System1.Stars[0]);
            Start2.XSystem = 1.0005;
            Start2.YSystem = 1.0005;



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

            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[0], 0);
            PlayerFaction2.TaskGroups[0].AddShip(PlayerFaction2.ShipDesigns[0], 0);

            PlayerFaction1.TaskGroups[0].Ships[0].Refuel(200000.0f);
            PlayerFaction2.TaskGroups[0].Ships[0].Refuel(200000.0f);


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

            System1.AddJumpPoint(S1, 0.1, 0.2);
            System2.AddJumpPoint(S2, 0.2, 0.1);

            System1.JumpPoints[0].Connect = System2.JumpPoints[0];
            System2.JumpPoints[0].Connect = System1.JumpPoints[0];

            System1.JumpPoints[0].Transit(PlayerFaction1.TaskGroups[0], false);
            System1.JumpPoints[0].Transit(PlayerFaction2.TaskGroups[0], true);

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
            Planet pl1 = new Planet(S1, S1);
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

            PlayerFaction1.TaskGroups[0].Ships[0].Refuel(200000.0f);

            Population P1 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);
            Population P2 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);

            System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number = 3.0f;
            System1.Stars[0].Planets[0].Populations[1].Installations[(int)Installation.InstallationType.Infrastructure].Number = 0.0f;

            Console.WriteLine("Infrastructure on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number,
                System1.Stars[0].Planets[0].Populations[1].Installations[(int)Installation.InstallationType.Infrastructure].Number);

            PlayerFaction1.TaskGroups[0].LoadCargo(System1.Stars[0].Planets[0].Populations[0], Installation.InstallationType.Infrastructure, 1);

            Console.WriteLine("Infrastructure on cargo tg after load in tons:{0}", PlayerFaction1.TaskGroups[0].Ships[0].CargoList[Installation.InstallationType.Infrastructure].tons);

            PlayerFaction1.TaskGroups[0].UnloadCargo(System1.Stars[0].Planets[0].Populations[1], Installation.InstallationType.Infrastructure, 1);

            Console.WriteLine("Infrastructure on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number,
    System1.Stars[0].Planets[0].Populations[1].Installations[(int)Installation.InstallationType.Infrastructure].Number);

            Console.WriteLine("CargoList dictionary count after unload:{0}", PlayerFaction1.TaskGroups[0].Ships[0].CargoList.Count);
        }

        [Test]
        public void CargoOrdersTest()
        {
            Faction PlayerFaction1 = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            Planet pl2 = new Planet(S1, S1);
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

            PlayerFaction1.TaskGroups[0].Ships[0].Refuel(200000.0f);

            Population P1 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);
            Population P2 = new Population(System1.Stars[0].Planets[1], PlayerFaction1);

            System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number = 3.0f;
            System1.Stars[0].Planets[1].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number = 0.0f;

            Orders Load = new Orders(Constants.ShipTN.OrderType.LoadInstallation, (int)Installation.InstallationType.Infrastructure, 1, 0, System1.Stars[0].Planets[0].Populations[0]);
            Orders Unload = new Orders(Constants.ShipTN.OrderType.UnloadInstallation, (int)Installation.InstallationType.Infrastructure, 1, 0, System1.Stars[0].Planets[1].Populations[0]);

            PlayerFaction1.TaskGroups[0].IssueOrder(Load);
            PlayerFaction1.TaskGroups[0].IssueOrder(Unload);


            Console.WriteLine("Infrastructure on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number,
                System1.Stars[0].Planets[1].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number);


            while (PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count > 0)
            {
                Console.WriteLine("Current Order Time: {0} {1}", PlayerFaction1.TaskGroups[0].TimeRequirement,
    PlayerFaction1.TaskGroups[0].TaskGroupOrders[0].orderTimeRequirement);

                PlayerFaction1.TaskGroups[0].FollowOrders(Constants.TimeInSeconds.TwentyMinutes);

                Console.WriteLine("Order Count: {0}", PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count);
            }

            Console.WriteLine("Infrastructure on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number,
    System1.Stars[0].Planets[1].Populations[0].Installations[(int)Installation.InstallationType.Infrastructure].Number);

            Console.WriteLine("CargoList count on Ships[0] after unload :{0}", PlayerFaction1.TaskGroups[0].Ships[0].CargoList.Count);
        }


        [Test]
        public void ColonyOrdersTest()
        {
            Faction PlayerFaction1 = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            Planet pl2 = new Planet(S1, S1);
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

            PlayerFaction1.TaskGroups[0].Ships[0].Refuel(200000.0f);

            Population P1 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);
            Population P2 = new Population(System1.Stars[0].Planets[1], PlayerFaction1);

            System1.Stars[0].Planets[0].Populations[0].CivilianPopulation = 5.0f;
            System1.Stars[0].Planets[1].Populations[0].CivilianPopulation = 1.0f;

            Orders Load = new Orders(Constants.ShipTN.OrderType.LoadColonists, -1, 9000, 0, System1.Stars[0].Planets[0].Populations[0]);
            Orders Unload = new Orders(Constants.ShipTN.OrderType.UnloadColonists, -1, 9000, 0, System1.Stars[0].Planets[1].Populations[0]);

            PlayerFaction1.TaskGroups[0].IssueOrder(Load);
            PlayerFaction1.TaskGroups[0].IssueOrder(Unload);

            while (PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count > 0)
            {
                Console.WriteLine("Current Order Time: {0} {1}", PlayerFaction1.TaskGroups[0].TimeRequirement,
    PlayerFaction1.TaskGroups[0].TaskGroupOrders[0].orderTimeRequirement);

                PlayerFaction1.TaskGroups[0].FollowOrders(Constants.TimeInSeconds.TwentyMinutes);

                Console.WriteLine("Order Count: {0}", PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count);
            }

            Console.WriteLine("Population on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].CivilianPopulation,
    System1.Stars[0].Planets[1].Populations[0].CivilianPopulation);
        }

        [Test]
        public void TaskGroupFuelOrdersTest()
        {
            Faction PlayerFaction1 = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            Planet pl2 = new Planet(S1, S1);
            Planet pl3 = new Planet(S1, S1);
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);
            System1.Stars[0].Planets.Add(pl2);
            System1.Stars[0].Planets.Add(pl3);

            System1.Stars[0].Planets[0].XSystem = 1.0;
            System1.Stars[0].Planets[0].YSystem = 1.0;

            System1.Stars[0].Planets[1].XSystem = 0.0;
            System1.Stars[0].Planets[1].YSystem = 3.0;

            System1.Stars[0].Planets[2].XSystem = 0.0;
            System1.Stars[0].Planets[2].YSystem = 0.0;


            PlayerFaction1.AddNewShipDesign("Blucher");
            PlayerFaction1.AddNewShipDesign("Tribal");
            PlayerFaction1.AddNewShipDesign("Ohio");

            PlayerFaction1.ShipDesigns[0].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[0].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[0].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 10);
            PlayerFaction1.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction1.ShipDesigns[0].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[0].IsTanker = true;

            PlayerFaction1.ShipDesigns[1].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[1].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[1].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 10);
            PlayerFaction1.ShipDesigns[1].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction1.ShipDesigns[1].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[1].IsTanker = true;

            PlayerFaction1.ShipDesigns[2].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[2].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[2].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 1);
            PlayerFaction1.ShipDesigns[2].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction1.ShipDesigns[2].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[2].IsTanker = false;

            PlayerFaction1.AddNewTaskGroup("P1 TG 01", System1.Stars[0].Planets[2], System1);
            PlayerFaction1.AddNewTaskGroup("P1 TG 02", System1.Stars[0].Planets[1], System1);
            PlayerFaction1.AddNewTaskGroup("P1 TG 03", System1.Stars[0].Planets[1], System1);

            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[0], 0);
            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[2], 0);
            PlayerFaction1.TaskGroups[1].AddShip(PlayerFaction1.ShipDesigns[1], 0);
            PlayerFaction1.TaskGroups[2].AddShip(PlayerFaction1.ShipDesigns[2], 0);

            PlayerFaction1.TaskGroups[0].Ships[0].Refuel(50000.0f);
            PlayerFaction1.TaskGroups[0].Ships[1].Refuel(50000.0f);
            PlayerFaction1.TaskGroups[1].Ships[0].Refuel(1000000.0f);

            Population P1 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);
            Population P2 = new Population(System1.Stars[0].Planets[1], PlayerFaction1);

            System1.Stars[0].Planets[0].Populations[0].FuelStockpile = 10000000.0f;
            System1.Stars[0].Planets[1].Populations[0].FuelStockpile = 10.0f;

            PlayerFaction1.TaskGroups[1].IsOrbiting = false;
            PlayerFaction1.TaskGroups[1].Contact.XSystem = 3.0;
            PlayerFaction1.TaskGroups[1].Contact.YSystem = 0.0;

            Orders RefuelFromColony = new Orders(Constants.ShipTN.OrderType.RefuelFromColony, -1, -1, -1, System1.Stars[0].Planets[0].Populations[0]);
            PlayerFaction1.TaskGroups[0].IssueOrder(RefuelFromColony);

            Orders RefuelFromTargetFleet = new Orders(Constants.ShipTN.OrderType.RefuelFromTargetFleet, -1, -1, -1, PlayerFaction1.TaskGroups[1]);
            PlayerFaction1.TaskGroups[0].IssueOrder(RefuelFromTargetFleet);

            Orders RefuelTargetFleet = new Orders(Constants.ShipTN.OrderType.RefuelTargetFleet, -1, -1, -1, PlayerFaction1.TaskGroups[2]);
            PlayerFaction1.TaskGroups[0].IssueOrder(RefuelTargetFleet);

            Orders RefuelFromOwnTankers = new Orders(Constants.ShipTN.OrderType.RefuelFromOwnTankers, -1, -1, -1, System1.Stars[0].Planets[1].Populations[0]);
            PlayerFaction1.TaskGroups[0].IssueOrder(RefuelFromOwnTankers);

            Orders UnloadFuelToPop = new Orders(Constants.ShipTN.OrderType.UnloadFuelToPlanet, -1, -1, -1, System1.Stars[0].Planets[1].Populations[0]);
            PlayerFaction1.TaskGroups[0].IssueOrder(UnloadFuelToPop);

            uint tickCount = 0;

            while (PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count > 0)
            {
                Console.WriteLine("===================={0} {1} {2}====================", tickCount, PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count, PlayerFaction1.TaskGroups[0].TimeRequirement);
                Console.WriteLine("X,Y: {0}/{1}", PlayerFaction1.TaskGroups[0].Contact.XSystem, PlayerFaction1.TaskGroups[0].Contact.YSystem);
                Console.WriteLine("Fuel:s1:{0} s2:{1} s3:{2} s4:{3} P1:{4} P2:{5}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentFuel, PlayerFaction1.TaskGroups[0].Ships[1].CurrentFuel,
                    PlayerFaction1.TaskGroups[1].Ships[0].CurrentFuel, PlayerFaction1.TaskGroups[2].Ships[0].CurrentFuel, System1.Stars[0].Planets[0].Populations[0].FuelStockpile,
                    System1.Stars[0].Planets[1].Populations[0].FuelStockpile);

                PlayerFaction1.TaskGroups[0].FollowOrders(Constants.TimeInSeconds.Hour);
                tickCount = tickCount + Constants.TimeInSeconds.Hour;
            }

            Console.WriteLine("===================={0} {1} {2}====================", tickCount, PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count, PlayerFaction1.TaskGroups[0].TimeRequirement);
            Console.WriteLine("X,Y: {0}/{1}", PlayerFaction1.TaskGroups[0].Contact.XSystem, PlayerFaction1.TaskGroups[0].Contact.YSystem);
            Console.WriteLine("Fuel:s1:{0} s2:{1} s3:{2} s4:{3} P1:{4} P2:{5}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentFuel, PlayerFaction1.TaskGroups[0].Ships[1].CurrentFuel,
                PlayerFaction1.TaskGroups[1].Ships[0].CurrentFuel, PlayerFaction1.TaskGroups[2].Ships[0].CurrentFuel, System1.Stars[0].Planets[0].Populations[0].FuelStockpile,
                System1.Stars[0].Planets[1].Populations[0].FuelStockpile);

        }


        [Test]
        public void TaskGroupMSPOrdersTest()
        {
            Faction PlayerFaction1 = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            Planet pl2 = new Planet(S1, S1);
            Planet pl3 = new Planet(S1, S1);
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);
            System1.Stars[0].Planets.Add(pl2);
            System1.Stars[0].Planets.Add(pl3);

            System1.Stars[0].Planets[0].XSystem = 1.0;
            System1.Stars[0].Planets[0].YSystem = 1.0;

            System1.Stars[0].Planets[1].XSystem = 0.0;
            System1.Stars[0].Planets[1].YSystem = 3.0;

            System1.Stars[0].Planets[2].XSystem = 0.0;
            System1.Stars[0].Planets[2].YSystem = 0.0;


            PlayerFaction1.AddNewShipDesign("Blucher");
            PlayerFaction1.AddNewShipDesign("Tribal");
            PlayerFaction1.AddNewShipDesign("Ohio");

            PlayerFaction1.ShipDesigns[0].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[0].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[0].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 2);
            PlayerFaction1.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 10);
            PlayerFaction1.ShipDesigns[0].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[0].IsSupply = true;

            PlayerFaction1.ShipDesigns[1].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[1].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[1].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 2);
            PlayerFaction1.ShipDesigns[1].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 10);
            PlayerFaction1.ShipDesigns[1].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[1].IsSupply = true;

            PlayerFaction1.ShipDesigns[2].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[2].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[2].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 2);
            PlayerFaction1.ShipDesigns[2].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction1.ShipDesigns[2].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[2].IsSupply = false;

            PlayerFaction1.AddNewTaskGroup("P1 TG 01", System1.Stars[0].Planets[2], System1);
            PlayerFaction1.AddNewTaskGroup("P1 TG 02", System1.Stars[0].Planets[1], System1);
            PlayerFaction1.AddNewTaskGroup("P1 TG 03", System1.Stars[0].Planets[1], System1);

            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[0], 0);
            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[2], 0);
            PlayerFaction1.TaskGroups[1].AddShip(PlayerFaction1.ShipDesigns[1], 0);
            PlayerFaction1.TaskGroups[2].AddShip(PlayerFaction1.ShipDesigns[2], 0);

            PlayerFaction1.TaskGroups[0].Ships[0].Refuel(100000.0f);
            PlayerFaction1.TaskGroups[0].Ships[1].Refuel(100000.0f);
            PlayerFaction1.TaskGroups[1].Ships[0].Refuel(100000.0f);

            PlayerFaction1.TaskGroups[0].Ships[0].CurrentMSP = 0;
            PlayerFaction1.TaskGroups[0].Ships[1].CurrentMSP = 0;
            PlayerFaction1.TaskGroups[2].Ships[0].CurrentMSP = 60;


            Population P1 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);
            Population P2 = new Population(System1.Stars[0].Planets[1], PlayerFaction1);

            System1.Stars[0].Planets[0].Populations[0].MaintenanceSupplies = 1000000;
            System1.Stars[0].Planets[1].Populations[0].MaintenanceSupplies = 10;

            PlayerFaction1.TaskGroups[1].IsOrbiting = false;
            PlayerFaction1.TaskGroups[1].Contact.XSystem = 3.0;
            PlayerFaction1.TaskGroups[1].Contact.YSystem = 0.0;

            Orders ResupplyFromColony = new Orders(Constants.ShipTN.OrderType.ResupplyFromColony, -1, -1, -1, System1.Stars[0].Planets[0].Populations[0]);
            PlayerFaction1.TaskGroups[0].IssueOrder(ResupplyFromColony);

            Orders ResupplyFromTargetFleet = new Orders(Constants.ShipTN.OrderType.ResupplyFromTargetFleet, -1, -1, -1, PlayerFaction1.TaskGroups[1]);
            PlayerFaction1.TaskGroups[0].IssueOrder(ResupplyFromTargetFleet);

            Orders ResupplyTargetFleet = new Orders(Constants.ShipTN.OrderType.ResupplyTargetFleet, -1, -1, -1, PlayerFaction1.TaskGroups[2]);
            PlayerFaction1.TaskGroups[0].IssueOrder(ResupplyTargetFleet);

            Orders ResupplyFromOwnSupplyShips = new Orders(Constants.ShipTN.OrderType.ResupplyFromOwnSupplyShips, -1, -1, -1, System1.Stars[0].Planets[1].Populations[0]);
            PlayerFaction1.TaskGroups[0].IssueOrder(ResupplyFromOwnSupplyShips);

            Orders UnloadSuppliesToPop = new Orders(Constants.ShipTN.OrderType.UnloadSuppliesToPlanet, -1, -1, -1, System1.Stars[0].Planets[1].Populations[0]);
            PlayerFaction1.TaskGroups[0].IssueOrder(UnloadSuppliesToPop);

            uint tickCount = 0;

            while (PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count > 0)
            {
                Console.WriteLine("===================={0} {1} {2}====================", tickCount, PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count, PlayerFaction1.TaskGroups[0].TimeRequirement);
                Console.WriteLine("X,Y: {0}/{1}", PlayerFaction1.TaskGroups[0].Contact.XSystem, PlayerFaction1.TaskGroups[0].Contact.YSystem);
                Console.WriteLine("MSP:s1:{0} s2:{1} s3:{2} s4:{3} P1:{4} P2:{5}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentMSP, PlayerFaction1.TaskGroups[0].Ships[1].CurrentMSP,
                    PlayerFaction1.TaskGroups[1].Ships[0].CurrentMSP, PlayerFaction1.TaskGroups[2].Ships[0].CurrentMSP, System1.Stars[0].Planets[0].Populations[0].MaintenanceSupplies,
                    System1.Stars[0].Planets[1].Populations[0].MaintenanceSupplies);

                PlayerFaction1.TaskGroups[0].FollowOrders(Constants.TimeInSeconds.Hour);
                tickCount = tickCount + Constants.TimeInSeconds.Hour;


            }

            Console.WriteLine("===================={0} {1} {2}====================", tickCount, PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count, PlayerFaction1.TaskGroups[0].TimeRequirement);
            Console.WriteLine("X,Y: {0}/{1}", PlayerFaction1.TaskGroups[0].Contact.XSystem, PlayerFaction1.TaskGroups[0].Contact.YSystem);
            Console.WriteLine("MSP:s1:{0} s2:{1} s3:{2} s4:{3} P1:{4} P2:{5}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentMSP, PlayerFaction1.TaskGroups[0].Ships[1].CurrentMSP,
                PlayerFaction1.TaskGroups[1].Ships[0].CurrentMSP, PlayerFaction1.TaskGroups[2].Ships[0].CurrentMSP, System1.Stars[0].Planets[0].Populations[0].MaintenanceSupplies,
                System1.Stars[0].Planets[1].Populations[0].MaintenanceSupplies);

        }


        [Test]
        public void ComponentLoadUnloadTest()
        {
            Faction PlayerFaction1 = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            Planet pl2 = new Planet(S1, S1);
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

            PlayerFaction1.TaskGroups[0].Ships[0].Refuel(200000.0f);

            Population P1 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);
            Population P2 = new Population(System1.Stars[0].Planets[1], PlayerFaction1);

            System1.Stars[0].Planets[0].Populations[0].AddComponentsToStockpile(PlayerFaction1.ComponentList.Engines[0], 500.0f);

            /// <summary>
            /// The 1st 0 after the ordertype is for the ComponentStockpile[0] and CargoComponentList[0] index respectively.
            /// </summary>
            Orders Load = new Orders(Constants.ShipTN.OrderType.LoadShipComponent, 0, 0, 0, System1.Stars[0].Planets[0].Populations[0]);
            Orders Unload = new Orders(Constants.ShipTN.OrderType.UnloadShipComponent, 0, 0, 0, System1.Stars[0].Planets[1].Populations[0]);

            PlayerFaction1.TaskGroups[0].IssueOrder(Load);
            PlayerFaction1.TaskGroups[0].IssueOrder(Unload);


            Console.WriteLine("Engine Components on on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].ComponentStockpileCount[0],
                System1.Stars[0].Planets[1].Populations[0].ComponentStockpileCount.Count);


            while (PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count > 0)
            {
                Console.WriteLine("Current Order Time: {0} {1}", PlayerFaction1.TaskGroups[0].TimeRequirement,
                   PlayerFaction1.TaskGroups[0].TaskGroupOrders[0].orderTimeRequirement);

                PlayerFaction1.TaskGroups[0].FollowOrders(Constants.TimeInSeconds.TwentyMinutes);

                Console.WriteLine("Order Count: {0}", PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count);
            }

            Console.WriteLine("Engine Components on on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].ComponentStockpileCount[0],
                System1.Stars[0].Planets[1].Populations[0].ComponentStockpileCount[0]);

            Console.WriteLine("CargoList count on Ships[0] after unload :{0}", PlayerFaction1.TaskGroups[0].Ships[0].CargoComponentList.Count);
        }

        [Test]
        public void ShipDamageModel()
        {
            DamageValuesTN.init();

            Faction PlayerFaction1 = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            Planet pl2 = new Planet(S1, S1);
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);
            System1.Stars[0].Planets.Add(pl2);

            System1.Stars[0].Planets[0].XSystem = 1.0;
            System1.Stars[0].Planets[0].YSystem = 1.0;

            System1.Stars[0].Planets[1].XSystem = 2.0;
            System1.Stars[0].Planets[1].YSystem = 2.0;


            PlayerFaction1.AddNewShipDesign("Blucher");

            PlayerFaction1.ShipDesigns[0].AddEngine(PlayerFaction1.ComponentList.Engines[0], 5);
            PlayerFaction1.ShipDesigns[0].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 5);
            PlayerFaction1.ShipDesigns[0].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 5);
            PlayerFaction1.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 5);
            PlayerFaction1.ShipDesigns[0].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 5);
            PlayerFaction1.ShipDesigns[0].AddActiveSensor(PlayerFaction1.ComponentList.ActiveSensorDef[0], 5);
            PlayerFaction1.ShipDesigns[0].AddBeamFireControl(PlayerFaction1.ComponentList.BeamFireControlDef[0], 5);
            PlayerFaction1.ShipDesigns[0].AddBeamWeapon(PlayerFaction1.ComponentList.BeamWeaponDef[0], 5);

            PlayerFaction1.ShipDesigns[0].NewArmor("Conventional", 2, 5);

            PlayerFaction1.AddNewTaskGroup("P1 TG 01", System1.Stars[0].Planets[0], System1);

            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[0], 0);

            PlayerFaction1.TaskGroups[0].Ships[0].Refuel(200000.0f);

            ushort Columns = PlayerFaction1.TaskGroups[0].Ships[0].ShipArmor.armorDef.cNum;
            Random Gen = new Random();
            ushort HitLocation = (ushort)Gen.Next(0, Columns);

            PlayerFaction1.TaskGroups[0].Ships[0].OnDamaged(DamageTypeTN.Missile, 4, HitLocation, PlayerFaction1.TaskGroups[0].Ships[0]);
            HitLocation = (ushort)Gen.Next(0, Columns);
            PlayerFaction1.TaskGroups[0].Ships[0].OnDamaged(DamageTypeTN.Missile, 4, HitLocation, PlayerFaction1.TaskGroups[0].Ships[0]);
            HitLocation = (ushort)Gen.Next(0, Columns);
            PlayerFaction1.TaskGroups[0].Ships[0].OnDamaged(DamageTypeTN.Missile, 4, HitLocation, PlayerFaction1.TaskGroups[0].Ships[0]);

            Console.WriteLine("Damage Template:");
            for (int loop = 0; loop < DamageValuesTN.MissileTable[3].damageTemplate.Count; loop++)
            {
                Console.WriteLine("{0} ", DamageValuesTN.MissileTable[3].damageTemplate[loop]);
            }

            Console.WriteLine("Armor:");
            for (int loop = 0; loop < PlayerFaction1.TaskGroups[0].Ships[0].ShipArmor.armorColumns.Count; loop++)
            {
                Console.WriteLine("{0} ", PlayerFaction1.TaskGroups[0].Ships[0].ShipArmor.armorColumns[loop]);
            }

            foreach (KeyValuePair<ushort, ushort> pair in PlayerFaction1.TaskGroups[0].Ships[0].ShipArmor.armorDamage)
            {
                Console.WriteLine("{0} {1} ", pair.Key, pair.Value);
            }

            int DAC = 1;
            for (int loop = 0; loop < PlayerFaction1.TaskGroups[0].Ships[0].ShipClass.ListOfComponentDefs.Count; loop++)
            {
                Console.WriteLine("{0} {1}-{2} {3}", PlayerFaction1.TaskGroups[0].Ships[0].ShipClass.ListOfComponentDefs[loop], DAC,
                    PlayerFaction1.TaskGroups[0].Ships[0].ShipClass.DamageAllocationChart[PlayerFaction1.TaskGroups[0].Ships[0].ShipClass.ListOfComponentDefs[loop]],
                    PlayerFaction1.TaskGroups[0].Ships[0].ComponentDefIndex[loop]);

                DAC = PlayerFaction1.TaskGroups[0].Ships[0].ShipClass.DamageAllocationChart[PlayerFaction1.TaskGroups[0].Ships[0].ShipClass.ListOfComponentDefs[loop]] + 1;
            }

            for (int loop2 = 0; loop2 < PlayerFaction1.TaskGroups[0].Ships[0].ShipComponents.Count; loop2++)
            {
                Console.WriteLine("{0} {1} {2}", loop2, PlayerFaction1.TaskGroups[0].Ships[0].ShipComponents[loop2], PlayerFaction1.TaskGroups[0].Ships[0].ShipComponents[loop2].componentIndex);
            }

            Console.WriteLine("{0} {1} {2} {3} {4} {5}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxEnginePower, PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxThermalSignature,
    PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxFuelUsePerHour, PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxSpeed, PlayerFaction1.TaskGroups[0].Ships[0].CurrentSpeed,
    PlayerFaction1.TaskGroups[0].Ships[0].ShipEngine[2].isDestroyed);

            int test = PlayerFaction1.TaskGroups[0].Ships[0].DestroyComponent(ComponentTypeTN.Engine, 0, 5, 2, Gen);

            Console.WriteLine("{0} {1} {2} {3} {4} {5} {6}", test, PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxEnginePower, PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxThermalSignature,
                PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxFuelUsePerHour, PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxSpeed, PlayerFaction1.TaskGroups[0].Ships[0].CurrentSpeed,
                PlayerFaction1.TaskGroups[0].Ships[0].ShipEngine[2].isDestroyed);

            PlayerFaction1.TaskGroups[0].Ships[0].RepairComponent(ComponentTypeTN.Engine, 22);

            Console.WriteLine("{0} {1} {2} {3} {4} {5}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxEnginePower, PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxThermalSignature,
    PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxFuelUsePerHour, PlayerFaction1.TaskGroups[0].Ships[0].CurrentMaxSpeed, PlayerFaction1.TaskGroups[0].Ships[0].CurrentSpeed,
    PlayerFaction1.TaskGroups[0].Ships[0].ShipEngine[2].isDestroyed);
        }




        /// <summary>
        /// create factions fills in the factions, tgs, and ships. The design for ships should be modified here.
        /// </summary>
        /// <param name="P">Factions</param>
        /// <param name="Sol">Starting starsystem</param>
        /// <param name="factionCount"># of factions</param>
        /// <param name="TGCount"># of tgs</param>
        /// <param name="ShipCount"># of ships</param>
        /// <param name="RNG">"global" rng since it has to be done that way.</param>
        void createFactions(BindingList<Faction> P, StarSystem Sol, int factionCount, int TGCount, int ShipCount, Random RNG)
        {
            for (int loop = 0; loop < factionCount; loop++)
            {
                Faction P1 = new Faction(loop);
                P1.AddNewContactList(Sol);
                P1.AddNewShipDesign("Blucher");

                P1.ShipDesigns[0].AddEngine(P1.ComponentList.Engines[0], 3);
                P1.ShipDesigns[0].AddCrewQuarters(P1.ComponentList.CrewQuarters[0], 1);
                P1.ShipDesigns[0].AddFuelStorage(P1.ComponentList.FuelStorage[0], 1);
                P1.ShipDesigns[0].AddEngineeringSpaces(P1.ComponentList.EngineeringSpaces[0], 1);
                P1.ShipDesigns[0].AddOtherComponent(P1.ComponentList.OtherComponents[0], 1);
                P1.ShipDesigns[0].AddActiveSensor(P1.ComponentList.ActiveSensorDef[0], 1);
                P1.ShipDesigns[0].AddBeamFireControl(P1.ComponentList.BeamFireControlDef[0], 1);
                P1.ShipDesigns[0].AddBeamWeapon(P1.ComponentList.BeamWeaponDef[0], 2);
                P1.ShipDesigns[0].AddReactor(P1.ComponentList.ReactorDef[0], 2);
                P1.ShipDesigns[0].NewArmor("Duranium", 5, 4);


                for (int loop2 = 0; loop2 < TGCount; loop2++)
                {
                    int randx = RNG.Next(0, 100000);
                    int randy = RNG.Next(0, 100000);

                    float wx = ((float)randx / 50000.0f) - 1.0f;
                    float wy = ((float)randy / 50000.0f) - 1.0f;

                    Planet Start = new Planet(Sol.Stars[0], Sol.Stars[0]);
                    Start.XSystem = wx;
                    Start.YSystem = wy;

                    string ID1 = loop.ToString();

                    string TGName = "P" + ID1 + "TG 01";

                    P1.AddNewTaskGroup(TGName, Start, Sol);

                    for (int loop3 = 0; loop3 < ShipCount; loop3++)
                    {
                        P1.TaskGroups[loop2].AddShip(P1.ShipDesigns[0], 0);
                        P1.TaskGroups[loop2].Ships[loop3].Refuel(200000.0f);
                        P1.TaskGroups[loop2].SetActiveSensor(loop3, 0, true);
                    }

                }
                P.Add(P1);
            }
        }

        /// <summary>
        /// InitShips stars all taskgroups moving towards the center of the map, and links all beam weapons(currently just 1 weapon to 1 bfc)
        /// </summary>
        /// <param name="P">Faction List</param>
        /// <param name="MoveToCenter">Order to move to center.</param>
        /// <param name="factionCount"># of factions.</param>
        /// <param name="TGCount"># of tgs.</param>
        /// <param name="ShipCount"># of ships</param>
        void initShips(BindingList<Faction> P, Orders MoveToCenter, int factionCount, int TGCount, int ShipCount)
        {
            for (int loop = 0; loop < factionCount; loop++)
            {
                for (int loop2 = 0; loop2 < TGCount; loop2++)
                {
                    P[loop].TaskGroups[loop2].IsOrbiting = false;
                    P[loop].TaskGroups[loop2].IssueOrder(MoveToCenter);

                    /// <summary>
                    /// Weapon linking is also handled here for the time being, adding more weapons will be problematic.
                    /// </summary>
                    for (int loop3 = 0; loop3 < ShipCount; loop3++)
                    {
                        P[loop].TaskGroups[loop2].Ships[loop3].LinkWeaponToBeamFC(P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0], P[loop].TaskGroups[loop2].Ships[loop3].ShipBeam[0]);
                        P[loop].TaskGroups[loop2].Ships[loop3].LinkWeaponToBeamFC(P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0], P[loop].TaskGroups[loop2].Ships[loop3].ShipBeam[1]);
                    }
                }
            }
        }

        /// <summary>
        /// Target Acquisition assigns new targets to ships that have destroyed their current target.
        /// </summary>
        /// <param name="P">Faction list.</param>
        /// <param name="factionCount"># of factions</param>
        void TargetAcquisition(BindingList<Faction> P, int factionCount)
        {
            for (int loop = 0; loop < factionCount; loop++)
            {
                for (int loop2 = 0; loop2 < P[loop].TaskGroups.Count; loop2++)
                {
                    for (int loop3 = 0; loop3 < P[loop].TaskGroups[loop2].Ships.Count; loop3++)
                    {

                        if (P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].target != null)
                        {
                            if (P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].target.ship.IsDestroyed == true)
                                P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].target = null;
                        }

                        if (P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].target == null && P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].isDestroyed == false)
                        {
                            ShipTN newTarget = P[loop].TaskGroups[loop2].getNewTarget();

                            if (newTarget != null)
                            {
                                P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].assignTarget(newTarget);
                                P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].openFire = true;


                                bool inOrderList = false;
                                for (int loop4 = 0; loop4 < P[loop].TaskGroups[loop2].TaskGroupOrders.Count; loop4++)
                                {
                                    if (P[loop].TaskGroups[loop2].TaskGroupOrders[loop4].target == newTarget.ShipsTaskGroup)
                                    {
                                        inOrderList = true;
                                        break;
                                    }
                                }

                                if (inOrderList == false)
                                {
                                    Orders MoveToTarget = new Orders(Constants.ShipTN.OrderType.MoveTo, 0, 0, 0, newTarget.ShipsTaskGroup);
                                    P[loop].TaskGroups[loop2].clearAllOrders();
                                    P[loop].TaskGroups[loop2].IssueOrder(MoveToTarget);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Fire Weapons fires each ship's weapons at their target, or if there is no target redirects the taskgroup to the center of the map.
        /// </summary>
        /// <param name="P">Factions</param>
        /// <param name="factionCount">Count of factions</param>
        /// <param name="MoveToCenter">Order to move to center of map</param>
        /// <param name="tick">Current sim tick</param>
        /// <param name="RNG">"Global" random.</param>
        /// <param name="done">boolean that determines if the simulation is finished.</param>
        /// <returns></returns>
        bool FireWeapons(BindingList<Faction> P, int factionCount, Orders MoveToCenter, int tick, Random RNG, bool done)
        {
            for (int loop = 0; loop < factionCount; loop++)
            {
                for (int loop2 = 0; loop2 < P[loop].TaskGroups.Count; loop2++)
                {
                    for (int loop3 = 0; loop3 < P[loop].TaskGroups[loop2].Ships.Count; loop3++)
                    {
                        if (P[loop].TaskGroups[loop2].Ships[loop3].IsDestroyed == false)
                        {
                            ShipTN target = P[loop].TaskGroups[loop2].Ships[loop3].ShipBFC[0].getTarget().ship;

                            if (target != null)
                            {
                                if (target.IsDestroyed == true)
                                {
                                    target = null;
                                    P[loop].TaskGroups[loop2].clearAllOrders();
                                    if (P[loop].TaskGroups[loop2].Contact.XSystem != 0.0 && P[loop].TaskGroups[loop2].Contact.YSystem != 0.0)
                                        P[loop].TaskGroups[loop2].IssueOrder(MoveToCenter);
                                }
                            }

                            if (target != null)
                            {
                                if (P[loop].DetectedContactLists.ContainsKey(target.ShipsTaskGroup.Contact.CurrentSystem))
                                {
                                    if (P[loop].DetectedContactLists[target.ShipsTaskGroup.Contact.CurrentSystem].DetectedContacts.ContainsKey(target))
                                    {
                                        if (P[loop].DetectedContactLists[target.ShipsTaskGroup.Contact.CurrentSystem].DetectedContacts[target].active == true)
                                        {
                                            if (P[loop].TaskGroups[loop2].Ships[loop3].IsDestroyed == false)
                                            {
                                                P[loop].TaskGroups[loop2].Ships[loop3].ShipFireWeapons(tick, RNG);
                                            }
                                        }
                                        else
                                        {
                                            P[loop].TaskGroups[loop2].clearAllOrders();
                                            if (P[loop].TaskGroups[loop2].Contact.XSystem != 0.0 && P[loop].TaskGroups[loop2].Contact.YSystem != 0.0)
                                                P[loop].TaskGroups[loop2].IssueOrder(MoveToCenter);
                                        }
                                    }
                                    else
                                    {
                                        P[loop].TaskGroups[loop2].clearAllOrders();
                                        if (P[loop].TaskGroups[loop2].Contact.XSystem != 0.0 && P[loop].TaskGroups[loop2].Contact.YSystem != 0.0)
                                            P[loop].TaskGroups[loop2].IssueOrder(MoveToCenter);
                                    }
                                }
                                int SC;
                                P[loop].TaskGroups[loop2].Ships[loop3].RechargeBeamWeapons(5, out SC);
                            }
                        }
                    }
                }

                /// <summary>
                /// Get ending condition here: no more targets anywhere.
                /// </summary>

                if (P[loop].TaskGroups.Count != 0)
                {
                    if (P[loop].DetectedContactLists[P[loop].TaskGroups[0].Contact.CurrentSystem].DetectedContacts.Count == 0 && P[loop].TaskGroups[0].TaskGroupOrders.Count == 0)
                    {
                        if (loop == (factionCount - 1) && done == true)
                        {
                            done = true;
                        }
                        else if (loop != (factionCount - 1))
                        {
                            done = true;
                        }
                        else
                        {
                            done = false;
                        }

                    }
                    else
                    {
                        done = false;
                    }
                }
                else
                {
                    done = true;
                }
                Console.WriteLine("***{0} {1} {2}***", loop, done, P[loop].DetectedContactLists[P[loop].TaskGroups[0].Contact.CurrentSystem].DetectedContacts.Count);
            }

            return done;
        }

        [Test]
        public void SimulationTest()
        {
            /// <summary>
            /// initialize damage and rng here:
            /// </summary>
            DamageValuesTN.init();
            Random RNG = new Random();

            StarSystem Sol = new StarSystem();

            BindingList<Faction> P = new BindingList<Faction>();
            int factionCount = 16;
            int TGCount = 10;
            int ShipCount = 8;

            /// <summary>
            /// Create all the factions here. later add new ships and tgs here as well.
            /// </summary>
            createFactions(P, Sol, factionCount, TGCount, ShipCount, RNG);


            /// <summary>
            /// Order every ship to proceed to the center.
            /// </summary>
            Waypoint Center = new Waypoint("WP Center", Sol, 0.0, 0.0, 0);

            Orders MoveToCenter = new Orders(Constants.ShipTN.OrderType.MoveTo, 0, 0, 0, Center);

            initShips(P, MoveToCenter, factionCount, TGCount, ShipCount);



            bool done = false;
            int tick = 5;
            int lastTick = 0;
            int ShipsDestroyed = 0;
            int TGDestroyed = 0;

            /// <summary>
            /// Run the simulation:
            /// </summary>
            while (!done)
            {
                Console.WriteLine("Tick {0} ShipsDestroyed {1} TGDestroyed {2} ", tick, ShipsDestroyed, TGDestroyed);
                /// <summary>
                /// Do sensor loop.
                /// Follow orders.
                /// I need to be able to know what targets are available. In system? per taskgroup?
                /// Dictionary in faction of system, and binding list of contacts?
                /// Attempt to fire.
                /// If one ship is destroyed exit loop.
                /// </summary>


                /// <summary>
                /// 1st do the sensor sweep:
                /// </summary>
                for (int loop = 0; loop < factionCount; loop++)
                {
                    P[loop].SensorSweep(tick);
                }


                /// <summary>
                /// Target selection:
                /// As with follow orders more taskgroups means another loop, likewise for if different ships in each TG want different targets.
                /// What conditions cause target loss? destruction of target and disappearance of target from sensors.
                /// </summary>
                TargetAcquisition(P, factionCount);


                /// <summary>
                /// Follow orders here.
                /// </summary>
                for (int loop = 0; loop < factionCount; loop++)
                {
                    for (int loop2 = 0; loop2 < P[loop].TaskGroups.Count; loop2++)
                    {
                        /// <summary>
                        /// Adding new taskgroups means adding a loop here to run through them all.
                        /// </summary>
                        if (P[loop].TaskGroups[loop2].TaskGroupOrders.Count != 0)
                            P[loop].TaskGroups[loop2].FollowOrders((uint)(tick - lastTick));
                    }
                }

                /// <summary>
                /// attempt to fire weapons at target here.
                /// Initiative will have to be implemented here for "fairness". right now lower P numbers have the advantage.
                /// </summary>
                //done = FireWeapons(P, factionCount, MoveToCenter, tick, RNG, done);



                /// <summary>
                /// Advance the game tick:
                /// </summary>
                lastTick = tick;
                tick += 5;

                /// <summary>
                /// Ending print report and preliminary ship/tg destruction handler.
                /// </summary>
                for (int loop = 0; loop < factionCount; loop++)
                {
                    for (int loop2 = 0; loop2 < P[loop].TaskGroups.Count; loop2++)
                    {
                        for (int loop3 = 0; loop3 < P[loop].TaskGroups[loop2].Ships.Count; loop3++)
                        {
                            if (P[loop].TaskGroups[loop2].Ships[loop3].ShipArmor.isDamaged == true)
                            {
                                Console.WriteLine("{0}{1}{2}", loop, loop2, loop3);
                                for (int loop4 = 0; loop4 < P[loop].TaskGroups[loop2].Ships[loop3].ShipArmor.armorColumns.Count; loop4++)
                                    Console.Write("{0} ", P[loop].TaskGroups[loop2].Ships[loop3].ShipArmor.armorColumns[loop4]);

                                Console.WriteLine("P{0}DC {1} | IsDestroyed:{2}", loop, P[loop].TaskGroups[loop2].Ships[loop3].DestroyedComponents.Count,
                                    P[loop].TaskGroups[loop2].Ships[loop3].IsDestroyed);
                            }

                            if (P[loop].TaskGroups[loop2].Ships[loop3].IsDestroyed == true)
                            {
                                for (int loop4 = 0; loop4 < factionCount; loop4++)
                                {
                                    StarSystem CurSystem = P[loop].TaskGroups[loop2].Contact.CurrentSystem;
                                    if (P[loop4].DetectedContactLists.ContainsKey(CurSystem))
                                    {
                                        if (P[loop4].DetectedContactLists[CurSystem].DetectedContacts.ContainsKey(P[loop].TaskGroups[loop2].Ships[loop3]))
                                        {
                                            P[loop4].DetectedContactLists[CurSystem].DetectedContacts.Remove(P[loop].TaskGroups[loop2].Ships[loop3]);
                                        }
                                    }
                                }
                                bool nodeGone = P[loop].TaskGroups[loop2].Ships[loop3].OnDestroyed();
                                P[loop].TaskGroups[loop2].Ships[loop3].ShipClass.ShipsInClass.Remove(P[loop].TaskGroups[loop2].Ships[loop3]);
                                P[loop].TaskGroups[loop2].Ships[loop3].ShipsTaskGroup.Ships.Remove(P[loop].TaskGroups[loop2].Ships[loop3]);

                                ShipsDestroyed++;

                                if (loop3 != (P[loop].TaskGroups[loop2].Ships.Count - 1))
                                    loop3--;

                                if (P[loop].TaskGroups[loop2].Ships.Count == 0)
                                {
                                    P[loop].TaskGroups[loop2].clearAllOrders();
                                    P[loop].TaskGroups[loop2].Contact.CurrentSystem.RemoveContact(P[loop].TaskGroups[loop2].Contact);
                                    P[loop].TaskGroups.Remove(P[loop].TaskGroups[loop2]);

                                    TGDestroyed++;

                                    if (loop2 != (P[loop].TaskGroups.Count - 1))
                                        loop2--;

                                    break;
                                }

                                P[loop].DetectedContactLists[P[loop].TaskGroups[loop2].Contact.CurrentSystem].DetectedContacts.Clear();
                            }
                        }
                        if (P[loop].TaskGroups.Count == 0)
                            break;
                    }
                }
            }
        }

        [Test]
        public void MesonMicrowaveShieldTest()
        {
            DamageValuesTN.init();

            Faction PlayerFaction1 = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            Planet pl2 = new Planet(S1, S1);
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);
            System1.Stars[0].Planets.Add(pl2);

            System1.Stars[0].Planets[0].XSystem = 1.0;
            System1.Stars[0].Planets[0].YSystem = 1.0;

            System1.Stars[0].Planets[1].XSystem = 2.0;
            System1.Stars[0].Planets[1].YSystem = 2.0;


            PlayerFaction1.AddNewShipDesign("Blucher");

            PlayerFaction1.ShipDesigns[0].AddEngine(PlayerFaction1.ComponentList.Engines[0], 5);
            PlayerFaction1.ShipDesigns[0].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 5);
            PlayerFaction1.ShipDesigns[0].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 5);
            PlayerFaction1.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 5);
            PlayerFaction1.ShipDesigns[0].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 5);
            PlayerFaction1.ShipDesigns[0].AddActiveSensor(PlayerFaction1.ComponentList.ActiveSensorDef[0], 1);
            PlayerFaction1.ShipDesigns[0].AddShield(PlayerFaction1.ComponentList.ShieldDef[0], 10);

            PlayerFaction1.ShipDesigns[0].NewArmor("Conventional", 2, 5);

            PlayerFaction1.AddNewTaskGroup("P1 TG 01", System1.Stars[0].Planets[0], System1);

            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[0], 0);

            PlayerFaction1.TaskGroups[0].Ships[0].Refuel(200000.0f);

            PlayerFaction1.TaskGroups[0].Ships[0].SetShields(true);

            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPool, PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPoolMax);

            PlayerFaction1.TaskGroups[0].Ships[0].RechargeShields(300);

            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPool, PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPoolMax);

            ushort Columns = PlayerFaction1.TaskGroups[0].Ships[0].ShipArmor.armorDef.cNum;
            Random Gen = new Random();
            ushort HitLocation = (ushort)Gen.Next(0, Columns);

            PlayerFaction1.TaskGroups[0].Ships[0].OnDamaged(DamageTypeTN.Microwave, 1, HitLocation, PlayerFaction1.TaskGroups[0].Ships[0]);

            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPool, PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPoolMax);

            HitLocation = (ushort)Gen.Next(0, Columns);

            PlayerFaction1.TaskGroups[0].Ships[0].OnDamaged(DamageTypeTN.Microwave, 1, HitLocation, PlayerFaction1.TaskGroups[0].Ships[0]);

            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPool, PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPoolMax);

            HitLocation = (ushort)Gen.Next(0, Columns);

            PlayerFaction1.TaskGroups[0].Ships[0].OnDamaged(DamageTypeTN.Microwave, 1, HitLocation, PlayerFaction1.TaskGroups[0].Ships[0]);

            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPool, PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPoolMax);

            HitLocation = (ushort)Gen.Next(0, Columns);

            PlayerFaction1.TaskGroups[0].Ships[0].OnDamaged(DamageTypeTN.Microwave, 1, HitLocation, PlayerFaction1.TaskGroups[0].Ships[0]);

            Console.WriteLine("{0} {1} {2}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPool, PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPoolMax,
                                             PlayerFaction1.TaskGroups[0].Ships[0].DestroyedComponents.Count);

            PlayerFaction1.TaskGroups[0].Ships[0].RechargeShields(300);

            Console.WriteLine("{0} {1}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPool, PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPoolMax);

            HitLocation = (ushort)Gen.Next(0, Columns);

            PlayerFaction1.TaskGroups[0].Ships[0].OnDamaged(DamageTypeTN.Meson, 1, HitLocation, PlayerFaction1.TaskGroups[0].Ships[0]);

            Console.WriteLine("{0} {1} {2}", PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPool, PlayerFaction1.TaskGroups[0].Ships[0].CurrentShieldPoolMax,
                                 PlayerFaction1.TaskGroups[0].Ships[0].DestroyedComponents.Count);
        }


        [Test]
        public void CollierOrdersTest()
        {
            Faction PlayerFaction1 = new Faction(0);

            StarSystem System1 = new StarSystem("Sol");

            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            Planet pl2 = new Planet(S1, S1);
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);
            System1.Stars[0].Planets.Add(pl2);

            System1.Stars[0].Planets[0].XSystem = 1.0;
            System1.Stars[0].Planets[0].YSystem = 1.0;

            System1.Stars[0].Planets[1].XSystem = 2.0;
            System1.Stars[0].Planets[1].YSystem = 2.0;


            PlayerFaction1.AddNewShipDesign("Blucher");

            MissileEngineDefTN TestMissileEngine = new MissileEngineDefTN("Testbed", 5.0f, 1.0f, 1.0f, 1.0f);
            OrdnanceSeriesTN Series = new OrdnanceSeriesTN("BLANK STANDIN");
            OrdnanceDefTN TestMissile = new OrdnanceDefTN("Test Missile", Series, 1.0f, 0, 1.0f, 1.0f, 0, 0.0f, 0, 0.0f, 0, 0.0f, 0, 0.0f, 0, 1, 0, 0.0f, 0.0f, 0, false, 0, false, 0, TestMissileEngine, 1);

            PlayerFaction1.ShipDesigns[0].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[0].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[0].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 2);
            PlayerFaction1.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction1.ShipDesigns[0].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[0].AddMagazine(PlayerFaction1.ComponentList.MagazineDef[0], 1);

            PlayerFaction1.ShipDesigns[0].SetPreferredOrdnance(TestMissile, 3);

            PlayerFaction1.AddNewTaskGroup("P1 TG 01", System1.Stars[0].Planets[0], System1);

            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[0], 0);

            PlayerFaction1.TaskGroups[0].Ships[0].Refuel(200000.0f);

            Population P1 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);
            Population P2 = new Population(System1.Stars[0].Planets[1], PlayerFaction1);

            System1.Stars[0].Planets[0].Populations[0].LoadMissileToStockpile(TestMissile, 4);


            Orders Load = new Orders(Constants.ShipTN.OrderType.LoadOrdnanceFromColony, -1, -1, 0, System1.Stars[0].Planets[0].Populations[0]);
            Orders Unload = new Orders(Constants.ShipTN.OrderType.UnloadOrdnanceToColony, -1, -1, 0, System1.Stars[0].Planets[1].Populations[0]);

            PlayerFaction1.TaskGroups[0].IssueOrder(Load);
            PlayerFaction1.TaskGroups[0].IssueOrder(Unload);

            bool CK = System1.Stars[0].Planets[1].Populations[0].MissileStockpile.ContainsKey(TestMissile);
            Console.WriteLine("Missiles on P1 and P2:{0} {1}", System1.Stars[0].Planets[0].Populations[0].MissileStockpile[TestMissile],
                CK);


            while (PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count > 0)
            {
                Console.WriteLine("Current Order Time: {0} {1}", PlayerFaction1.TaskGroups[0].TimeRequirement,
    PlayerFaction1.TaskGroups[0].TaskGroupOrders[0].orderTimeRequirement);

                PlayerFaction1.TaskGroups[0].FollowOrders(Constants.TimeInSeconds.TwentyMinutes);

                Console.WriteLine("Order Count: {0}", PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count);
            }

            bool CK1 = System1.Stars[0].Planets[0].Populations[0].MissileStockpile.ContainsKey(TestMissile);
            bool CK2 = System1.Stars[0].Planets[1].Populations[0].MissileStockpile.ContainsKey(TestMissile);
            Console.WriteLine("Missiles on P1 and P2:{0} {1}", CK1,
                            CK2);

            if (CK1 == true)
            {
                Console.WriteLine("P1 Missiles {0}", System1.Stars[0].Planets[0].Populations[0].MissileStockpile[TestMissile]);
            }

            if (CK2 == true)
            {
                Console.WriteLine("P2 Missiles {0}", System1.Stars[0].Planets[1].Populations[0].MissileStockpile[TestMissile]);
            }

            CK = PlayerFaction1.TaskGroups[0].Ships[0].ShipOrdnance.ContainsKey(TestMissile);
            Console.WriteLine("Missile count on Ships[0] after unload :{0}", CK);
        }


        [Test]
        public void OrdnanceTest()
        {
            /// <summary>
            /// Need to hook missiles into the distance table calculations, as well as sensor model.
            /// </summary>


            /// <summary>
            ///The Damage table MUST be initialized.
            /// </summary>
            DamageValuesTN.init();

            /// <summary>
            /// Factions ARE necessary.
            /// </summary>
            Faction PlayerFaction1 = new Faction(0);
            Faction PlayerFaction2 = new Faction(1);

            /// <summary>
            /// No StarSystem no contacts!
            /// </summary>
            StarSystem System1 = new StarSystem("Sol");
            PlayerFaction1.AddNewContactList(System1);
            PlayerFaction2.AddNewContactList(System1);

            /// <summary>
            /// No global RNG, no Damage or tohit.
            /// </summary>
            Random RNG = new Random();

            /// <summary>
            /// Planets and populations are needed for house keeping.
            /// </summary>
            Star S1 = new Star();
            Planet pl1 = new Planet(S1, S1);
            Planet pl2 = new Planet(S1, S1);
            System1.Stars.Add(S1);
            System1.Stars[0].Planets.Add(pl1);
            System1.Stars[0].Planets.Add(pl2);

            Population P1 = new Population(System1.Stars[0].Planets[0], PlayerFaction1);
            Population P2 = new Population(System1.Stars[0].Planets[1], PlayerFaction2);

            System1.Stars[0].Planets[0].XSystem = 1.0;
            System1.Stars[0].Planets[0].YSystem = 1.0;

            System1.Stars[0].Planets[1].XSystem = 1.05;
            System1.Stars[0].Planets[1].YSystem = 1.05;


            PlayerFaction1.AddNewShipDesign("Blucher");
            PlayerFaction2.AddNewShipDesign("Tribal");

            MissileEngineDefTN TestMissileEngine = new MissileEngineDefTN("Testbed", 5.0f, 4.0f, 1.0f, 1.0f);

            OrdnanceSeriesTN Series = new OrdnanceSeriesTN("BLANK STANDIN");
            OrdnanceDefTN TestMissile = new OrdnanceDefTN("Test Missile", Series, 1.0f, 0, 1.0f, 1.0f, 0, 0.0f, 0, 0.0f, 0, 0.0f, 0, 0.0f, 0, 1, 0, 0.0f, 0.0f, 0, false, 0, false, 0, TestMissileEngine, 1);

            ActiveSensorDefTN Spotter = new ActiveSensorDefTN("Spotter", 5.0f, 10, 5, 18, false, 1.0f, 0);

            PlayerFaction1.ShipDesigns[0].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction1.ShipDesigns[0].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction1.ShipDesigns[0].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 2);
            PlayerFaction1.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction1.ShipDesigns[0].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction1.ShipDesigns[0].AddMagazine(PlayerFaction1.ComponentList.MagazineDef[0], 1);
            PlayerFaction1.ShipDesigns[0].AddLauncher(PlayerFaction1.ComponentList.MLauncherDef[0], 1);
            PlayerFaction1.ShipDesigns[0].AddMFC(PlayerFaction1.ComponentList.MissileFireControlDef[0], 1);
            PlayerFaction1.ShipDesigns[0].AddActiveSensor(Spotter, 1);

            PlayerFaction2.ShipDesigns[0].AddEngine(PlayerFaction1.ComponentList.Engines[0], 1);
            PlayerFaction2.ShipDesigns[0].AddCrewQuarters(PlayerFaction1.ComponentList.CrewQuarters[0], 2);
            PlayerFaction2.ShipDesigns[0].AddFuelStorage(PlayerFaction1.ComponentList.FuelStorage[0], 2);
            PlayerFaction2.ShipDesigns[0].AddEngineeringSpaces(PlayerFaction1.ComponentList.EngineeringSpaces[0], 2);
            PlayerFaction2.ShipDesigns[0].AddOtherComponent(PlayerFaction1.ComponentList.OtherComponents[0], 1);
            PlayerFaction2.ShipDesigns[0].NewArmor("Duranium", 5, 4);

            PlayerFaction1.ShipDesigns[0].SetPreferredOrdnance(TestMissile, 3);

            PlayerFaction1.AddNewTaskGroup("P1 TG 01", System1.Stars[0].Planets[0], System1);
            PlayerFaction2.AddNewTaskGroup("P2 TG 01", System1.Stars[0].Planets[1], System1);

            PlayerFaction1.TaskGroups[0].AddShip(PlayerFaction1.ShipDesigns[0], 0);
            PlayerFaction2.TaskGroups[0].AddShip(PlayerFaction2.ShipDesigns[0], 0);


            PlayerFaction1.TaskGroups[0].Ships[0].Refuel(200000.0f);
            PlayerFaction2.TaskGroups[0].Ships[0].Refuel(200000.0f);

            System1.Stars[0].Planets[0].Populations[0].LoadMissileToStockpile(TestMissile, 4);

            Orders Load = new Orders(Constants.ShipTN.OrderType.LoadOrdnanceFromColony, -1, -1, 0, System1.Stars[0].Planets[0].Populations[0]);

            PlayerFaction1.TaskGroups[0].IssueOrder(Load);

            while (PlayerFaction1.TaskGroups[0].TaskGroupOrders.Count > 0)
            {
                PlayerFaction1.TaskGroups[0].FollowOrders(Constants.TimeInSeconds.TwentyMinutes);
            }

            /// <summary>
            /// Magazine loading isn't handled anywhere.
            /// </summary>
            PlayerFaction1.TaskGroups[0].Ships[0].ShipMLaunchers[0].loadedOrdnance = TestMissile;

            PlayerFaction1.TaskGroups[0].Ships[0].ShipMLaunchers[0].AssignMFC(PlayerFaction1.TaskGroups[0].Ships[0].ShipMFC[0]);

            PlayerFaction1.TaskGroups[0].Ships[0].ShipMFC[0].assignTarget(PlayerFaction2.TaskGroups[0].Ships[0]);
            PlayerFaction1.TaskGroups[0].Ships[0].ShipMFC[0].openFire = true;
            PlayerFaction1.TaskGroups[0].SetActiveSensor(0, 0, true);

            PlayerFaction1.SensorSweep(5);

            PlayerFaction1.TaskGroups[0].Ships[0].ShipFireWeapons(5, RNG);

            uint tick = 10;

            bool done = false;
            while (!done)
            {
                Console.WriteLine("{0}", tick);
                PlayerFaction1.SensorSweep((int)tick);
                PlayerFaction1.MissileGroups[0].ProcessOrder(tick, RNG);

                Console.WriteLine("{0} {1} {2} {3} {4} {5}", PlayerFaction1.MissileGroups[0].currentHeading, PlayerFaction1.MissileGroups[0].currentSpeedX,
                    PlayerFaction1.MissileGroups[0].currentSpeedY, PlayerFaction1.MissileGroups[0].timeReq, PlayerFaction1.MissileGroups[0].dx, PlayerFaction1.MissileGroups[0].dy);

                tick = tick + 5;

                if (PlayerFaction1.MissileGroups[0].missiles.Count == 0)
                {
                    PlayerFaction1.MissileGroups.Clear();
                    done = true;
                }
            }


            Console.WriteLine("Armor:");
            for (int loop = 0; loop < PlayerFaction2.TaskGroups[0].Ships[0].ShipArmor.armorColumns.Count; loop++)
            {
                Console.WriteLine("{0} ", PlayerFaction2.TaskGroups[0].Ships[0].ShipArmor.armorColumns[loop]);
            }

        }
    }
}