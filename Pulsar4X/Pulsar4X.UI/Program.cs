﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

using Pulsar4X.Entities.Components;
using Pulsar4X.ECSLib;

namespace Pulsar4X.UI
{
    static class Program
    {
#if LOG4NET_ENABLED
        public static readonly ILog logger = LogManager.GetLogger(typeof(Program));
#endif

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [MTAThread]
        static void Main()
        {
#if LOG4NET_ENABLED
            XmlConfigurator.Configure(); // Enables Log4net based on App.config file.
            logger.Info("Program Started");
#endif

            // gen star system:
            Entities.StarSystem sol = SystemGen.CreateSol();
            SystemGen.CreateStressTest();
            SystemGen.CreateSystem("Test");
            SystemGen.CreateSystem("Foo");
            SystemGen.CreateSystem("Bar");



            /// <summary>
            /// Alpha Build related area:
            /// </summary>


            // gen Faction and a pop:
            Entities.Faction oNewFaction = new Entities.Faction(0);

            /// <summary>
            /// This following section should probably be moved to the Faction constructor at some point.
            /// </summary>
            oNewFaction.Populations.Add(new Entities.Population(sol.Stars.FirstOrDefault().Planets[2], oNewFaction, 0)); //.FirstOrDefault() for planets.
            oNewFaction.Populations[0].Planet.HomeworldMineralGeneration();
            oNewFaction.Populations[0].ConventionalStart();
            /// <summary>
            /// Add Contact lists.
            /// </summary>
            foreach (Entities.StarSystem CurSystem in GameState.Instance.StarSystems)
            {
                oNewFaction.AddNewContactList(CurSystem);
            }
            Entities.SystemBody P1 = new Entities.SystemBody(sol.Stars[0], Entities.SystemBody.PlanetType.Comet);  ///< @tdo WTF??? Alpha Build Related Area fat fingering ^
            P1.Position.X = 10.0;
            P1.Position.Y = 10.0;

            oNewFaction.Capitol = oNewFaction.Populations[0];
            oNewFaction.Capitol.Planet.GeoSurveyList.Add(oNewFaction, true);
            oNewFaction.AddNewTaskGroup("Combat Taskgroup  HR", P1, sol);
            oNewFaction.FactionColor = System.Drawing.Color.Blue;

            Entities.Faction oNewFaction2 = new Entities.Faction(1);
            oNewFaction2.Name = "Terran Federation";
            oNewFaction2.Populations.Add(new Entities.Population(sol.Stars.FirstOrDefault().Planets[2], oNewFaction2, 0)); //.FirstOrDefault()
            oNewFaction2.Populations[0].Planet.HomeworldMineralGeneration();
            oNewFaction2.Populations[0].ConventionalStart();

            /// <summary>
            /// Add Contact lists.
            /// </summary>
            foreach (Entities.StarSystem CurSystem in GameState.Instance.StarSystems)
            {
                oNewFaction2.AddNewContactList(CurSystem);
            }

            Entities.SystemBody P2 = new Entities.SystemBody(sol.Stars[0], Entities.SystemBody.PlanetType.Comet);  ///< wtf??? its a fatfinger to initialize the position of Combat Taskgroup TR
            P2.Position.X = -10.0;
            P2.Position.Y = -10.0;


            oNewFaction2.Capitol = oNewFaction2.Populations[0];
            oNewFaction2.Capitol.Planet.GeoSurveyList.Add(oNewFaction2, true);
            oNewFaction2.AddNewTaskGroup("Combat Taskgroup TR", P2, sol);
            oNewFaction2.FactionColor = System.Drawing.Color.Red;

            oNewFaction.AddNewShipDesign("Sword");
            ComponentDefListTN CL = oNewFaction.ComponentList;


            oNewFaction.ShipDesigns[0].AddEngine(CL.Engines[1], 8);
            oNewFaction.ShipDesigns[0].AddCrewQuarters(CL.CrewQuarters[0], 11);
            oNewFaction.ShipDesigns[0].AddFuelStorage(CL.FuelStorage[0], 10);
            oNewFaction.ShipDesigns[0].AddEngineeringSpaces(CL.EngineeringSpaces[0], 10);
            oNewFaction.ShipDesigns[0].AddOtherComponent(CL.OtherComponents[0], 1);
            oNewFaction.ShipDesigns[0].AddActiveSensor(CL.ActiveSensorDef[1], 1);
            oNewFaction.ShipDesigns[0].AddActiveSensor(CL.ActiveSensorDef[2], 1);
            oNewFaction.ShipDesigns[0].AddActiveSensor(CL.ActiveSensorDef[3], 1);
            oNewFaction.ShipDesigns[0].AddPassiveSensor(CL.PassiveSensorDef[2], 1);
            oNewFaction.ShipDesigns[0].AddPassiveSensor(CL.PassiveSensorDef[3], 1);
            oNewFaction.ShipDesigns[0].AddReactor(CL.ReactorDef[1], 10);
            oNewFaction.ShipDesigns[0].AddBeamFireControl(CL.BeamFireControlDef[1], 2);
            oNewFaction.ShipDesigns[0].AddShield(CL.ShieldDef[1], 20);
            oNewFaction.ShipDesigns[0].IsLocked = true;

            for (int loop = 1; loop < 8; loop++)
            {
                oNewFaction.ShipDesigns[0].AddBeamWeapon(CL.BeamWeaponDef[loop], 1);
            }
            oNewFaction.ShipDesigns[0].NewArmor("Composite", 8, 4);

            oNewFaction.AddNewShipDesign("Hammer");
            oNewFaction.ShipDesigns[1].AddEngine(CL.Engines[2], 2);
            oNewFaction.ShipDesigns[1].AddCrewQuarters(CL.CrewQuarters[0], 8);
            oNewFaction.ShipDesigns[1].AddFuelStorage(CL.FuelStorage[0], 10);
            oNewFaction.ShipDesigns[1].AddEngineeringSpaces(CL.EngineeringSpaces[0], 10);
            oNewFaction.ShipDesigns[1].AddOtherComponent(CL.OtherComponents[0], 1);
            oNewFaction.ShipDesigns[1].AddActiveSensor(CL.ActiveSensorDef[4], 1);
            oNewFaction.ShipDesigns[1].AddActiveSensor(CL.ActiveSensorDef[5], 1);
            oNewFaction.ShipDesigns[1].AddMFC(CL.MissileFireControlDef[1], 1);
            oNewFaction.ShipDesigns[1].AddMFC(CL.MissileFireControlDef[2], 1);
            oNewFaction.ShipDesigns[1].AddMagazine(CL.MagazineDef[1], 1);
            oNewFaction.ShipDesigns[1].AddLauncher(CL.MLauncherDef[1], 4);
            oNewFaction.ShipDesigns[1].AddLauncher(CL.MLauncherDef[2], 4);
            oNewFaction.ShipDesigns[1].NewArmor("Collapsium", 45, 5);
            oNewFaction.ShipDesigns[1].SetPreferredOrdnance(CL.MissileDef[0], 100);
            oNewFaction.ShipDesigns[1].SetPreferredOrdnance(CL.MissileDef[1], 200);
            oNewFaction.ShipDesigns[1].IsLocked = true;
            oNewFaction.ShipDesigns[1].BuildClassSummary();


            oNewFaction.TaskGroups.Last().AddShip(oNewFaction.ShipDesigns[0], "Warship");
            oNewFaction.TaskGroups.Last().AddShip(oNewFaction.ShipDesigns[0], "Backup Warship");
            oNewFaction.TaskGroups.Last().AddShip(oNewFaction.ShipDesigns[1], "Hammer");
            oNewFaction.TaskGroups.Last().Ships[0].Refuel(1000000.0f);
            oNewFaction.TaskGroups.Last().Ships[1].Refuel(1000000.0f);
            oNewFaction.TaskGroups.Last().Ships[2].Refuel(1000000.0f);

            foreach (KeyValuePair<OrdnanceDefTN, int> pair in oNewFaction.ShipDesigns[1].ShipClassOrdnance)
            {
                oNewFaction.TaskGroups.Last().Ships[2].ShipOrdnance.Add(pair.Key, pair.Value);
            }
            oNewFaction.TaskGroups.Last().Ships[2].CurrentMagazineCapacity = oNewFaction.ShipDesigns[1].PreferredOrdnanceSize;

            oNewFaction2.AddNewShipDesign("Mace");
            CL = oNewFaction2.ComponentList;


            oNewFaction2.ShipDesigns[0].AddEngine(CL.Engines[1], 8);
            oNewFaction2.ShipDesigns[0].AddCrewQuarters(CL.CrewQuarters[0], 11);
            oNewFaction2.ShipDesigns[0].AddFuelStorage(CL.FuelStorage[0], 10);
            oNewFaction2.ShipDesigns[0].AddEngineeringSpaces(CL.EngineeringSpaces[0], 10);
            oNewFaction2.ShipDesigns[0].AddOtherComponent(CL.OtherComponents[0], 1);
            oNewFaction2.ShipDesigns[0].AddActiveSensor(CL.ActiveSensorDef[1], 1);
            oNewFaction2.ShipDesigns[0].AddActiveSensor(CL.ActiveSensorDef[2], 1);
            oNewFaction2.ShipDesigns[0].AddActiveSensor(CL.ActiveSensorDef[3], 1);
            oNewFaction2.ShipDesigns[0].AddPassiveSensor(CL.PassiveSensorDef[2], 1);
            oNewFaction2.ShipDesigns[0].AddPassiveSensor(CL.PassiveSensorDef[3], 1);
            oNewFaction2.ShipDesigns[0].AddReactor(CL.ReactorDef[1], 10);
            oNewFaction2.ShipDesigns[0].AddBeamFireControl(CL.BeamFireControlDef[1], 2);
            oNewFaction2.ShipDesigns[0].AddShield(CL.ShieldDef[1], 20);
            oNewFaction2.ShipDesigns[0].IsLocked = true;

            for (int loop = 1; loop < 8; loop++)
            {
                oNewFaction2.ShipDesigns[0].AddBeamWeapon(CL.BeamWeaponDef[loop], 1);
            }
            oNewFaction2.ShipDesigns[0].NewArmor("Composite", 8, 4);

            oNewFaction2.AddNewShipDesign("Crusher");
            oNewFaction2.ShipDesigns[1].AddEngine(CL.Engines[2], 2);
            oNewFaction2.ShipDesigns[1].AddCrewQuarters(CL.CrewQuarters[0], 8);
            oNewFaction2.ShipDesigns[1].AddFuelStorage(CL.FuelStorage[0], 10);
            oNewFaction2.ShipDesigns[1].AddEngineeringSpaces(CL.EngineeringSpaces[0], 10);
            oNewFaction2.ShipDesigns[1].AddOtherComponent(CL.OtherComponents[0], 1);
            oNewFaction2.ShipDesigns[1].AddActiveSensor(CL.ActiveSensorDef[4], 1);
            oNewFaction2.ShipDesigns[1].AddActiveSensor(CL.ActiveSensorDef[5], 1);
            oNewFaction2.ShipDesigns[1].AddMFC(CL.MissileFireControlDef[1], 1);
            oNewFaction2.ShipDesigns[1].AddMFC(CL.MissileFireControlDef[2], 1);
            oNewFaction2.ShipDesigns[1].AddMagazine(CL.MagazineDef[1], 1);
            oNewFaction2.ShipDesigns[1].AddLauncher(CL.MLauncherDef[1], 4);
            oNewFaction2.ShipDesigns[1].AddLauncher(CL.MLauncherDef[2], 4);
            oNewFaction2.ShipDesigns[1].NewArmor("Collapsium", 45, 5);
            oNewFaction2.ShipDesigns[1].SetPreferredOrdnance(CL.MissileDef[0], 100);
            oNewFaction2.ShipDesigns[1].SetPreferredOrdnance(CL.MissileDef[1], 200);
            oNewFaction2.ShipDesigns[1].IsLocked = true;
            oNewFaction2.ShipDesigns[1].BuildClassSummary();

            oNewFaction2.TaskGroups.Last().AddShip(oNewFaction2.ShipDesigns[0], "Battleship");
            oNewFaction2.TaskGroups.Last().AddShip(oNewFaction2.ShipDesigns[0], "Backup Battleship");
            oNewFaction2.TaskGroups.Last().AddShip(oNewFaction.ShipDesigns[1], "Crusher");
            oNewFaction2.TaskGroups.Last().Ships[0].Refuel(1000000.0f);
            oNewFaction2.TaskGroups.Last().Ships[1].Refuel(1000000.0f);
            oNewFaction2.TaskGroups.Last().Ships[2].Refuel(1000000.0f);

            foreach (KeyValuePair<OrdnanceDefTN, int> pair in oNewFaction2.ShipDesigns[1].ShipClassOrdnance)
            {
                oNewFaction2.TaskGroups.Last().Ships[2].ShipOrdnance.Add(pair.Key, pair.Value);
            }
            oNewFaction2.TaskGroups.Last().Ships[2].CurrentMagazineCapacity = oNewFaction2.ShipDesigns[1].PreferredOrdnanceSize;

            oNewFaction.GiveAllTechs();
            oNewFaction2.GiveAllTechs();

            /// <summary>
            /// End Alpha.
            /// </summary>







            Application.EnableVisualStyles();

            //Initialize damage values.
            DamageValuesTN.init();


            // Init our UI Controller:
            Helpers.UIController.Instance.Initialise();
            //Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Forms.MainForm());

#if LOG4NET_ENABLED
            logger.Info("Program Ended");
#endif
        }
    }
}
