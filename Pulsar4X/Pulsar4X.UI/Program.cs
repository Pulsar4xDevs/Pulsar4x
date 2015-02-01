using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Pulsar4X.Stargen;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

using Pulsar4X.Entities.Components;

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
        [STAThread]
        static void Main()
        {
#if LOG4NET_ENABLED
            XmlConfigurator.Configure(); // Enables Log4net based on App.config file.
            logger.Info("Program Started");
#endif

            // gen star system:
            Entities.StarSystem otest = GameState.Instance.StarSystemFactory.Create("Test");
            GameState.Instance.StarSystemCurrentIndex++;
            GameState.Instance.StarSystemFactory.Create("Foo");
            GameState.Instance.StarSystemCurrentIndex++;
            GameState.Instance.StarSystemFactory.Create("Bar");
            GameState.Instance.StarSystemCurrentIndex++;



            /// <summary>
            /// Alpha Build related area:
            /// </summary>


            // gen Faction and a pop:
            Entities.Faction oNewFaction = new Entities.Faction(0);

            /// <summary>
            /// This following section should probably be moved to the Faction constructor at some point.
            /// </summary>
            oNewFaction.Populations.Add(new Entities.Population(otest.Stars.FirstOrDefault().Planets.FirstOrDefault(), oNewFaction));
            oNewFaction.Populations[0].Planet.HomeworldMineralGeneration();
            oNewFaction.Populations[0].ConventionalStart();
            GameState.Instance.Factions.Add(oNewFaction);
            /// <summary>
            /// Add Contact lists.
            /// </summary>
            foreach (Entities.StarSystem CurSystem in GameState.Instance.StarSystems)
            {
                oNewFaction.AddNewContactList(CurSystem);
            }
            Entities.Planet P1 = new Entities.Planet(otest.Stars[0], otest.Stars[0]);
            P1.Position.X = 10.0;
            P1.Position.Y = 10.0;

            oNewFaction.Capitol = oNewFaction.Populations[0].Planet;
            oNewFaction.Capitol.GeoSurveyList.Add(oNewFaction, true);
            oNewFaction.AddNewTaskGroup("Combat Taskgroup  HR", P1, otest);
            oNewFaction.FactionColor = System.Drawing.Color.Blue;

            Entities.Faction oNewFaction2 = new Entities.Faction(1);
            oNewFaction2.Name = "Terran Federation";
            oNewFaction2.Populations.Add(new Entities.Population(otest.Stars.FirstOrDefault().Planets.FirstOrDefault(), oNewFaction2));
            oNewFaction2.Populations[0].Planet.HomeworldMineralGeneration();
            oNewFaction2.Populations[0].ConventionalStart();
            GameState.Instance.Factions.Add(oNewFaction2);

            /// <summary>
            /// Add Contact lists.
            /// </summary>
            foreach (Entities.StarSystem CurSystem in GameState.Instance.StarSystems)
            {
                oNewFaction2.AddNewContactList(CurSystem);
            }

            Entities.Planet P2 = new Entities.Planet(otest.Stars[0], otest.Stars[0]);
            P2.Position.X = -10.0;
            P2.Position.Y = -10.0;


            oNewFaction2.Capitol = oNewFaction2.Populations[0].Planet;
            oNewFaction2.Capitol.GeoSurveyList.Add(oNewFaction2, true);
            oNewFaction2.AddNewTaskGroup("Combat Taskgroup TR", P2, otest);
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


            oNewFaction.TaskGroups[0].AddShip(oNewFaction.ShipDesigns[0]);
            oNewFaction.TaskGroups[0].AddShip(oNewFaction.ShipDesigns[0]);
            oNewFaction.TaskGroups[0].AddShip(oNewFaction.ShipDesigns[1]);
            oNewFaction.TaskGroups[0].Ships[0].Refuel(1000000.0f);
            oNewFaction.TaskGroups[0].Ships[1].Refuel(1000000.0f);
            oNewFaction.TaskGroups[0].Ships[2].Refuel(1000000.0f);

            oNewFaction.TaskGroups[0].Ships[0].Name = "Warship";
            oNewFaction.TaskGroups[0].Ships[1].Name = "Backup Warship";
            oNewFaction.TaskGroups[0].Ships[2].Name = "Hammer";
            foreach (KeyValuePair<OrdnanceDefTN, int> pair in oNewFaction.ShipDesigns[1].ShipClassOrdnance)
            {
                oNewFaction.TaskGroups[0].Ships[2].ShipOrdnance.Add(pair.Key, pair.Value);
            }
            oNewFaction.TaskGroups[0].Ships[2].CurrentMagazineCapacity = oNewFaction.ShipDesigns[1].PreferredOrdnanceSize;

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

            oNewFaction2.TaskGroups[0].AddShip(oNewFaction2.ShipDesigns[0]);
            oNewFaction2.TaskGroups[0].AddShip(oNewFaction2.ShipDesigns[0]);
            oNewFaction2.TaskGroups[0].AddShip(oNewFaction.ShipDesigns[1]);
            oNewFaction2.TaskGroups[0].Ships[0].Refuel(1000000.0f);
            oNewFaction2.TaskGroups[0].Ships[1].Refuel(1000000.0f);
            oNewFaction2.TaskGroups[0].Ships[2].Refuel(1000000.0f);

            oNewFaction2.TaskGroups[0].Ships[0].Name = "Battleship";
            oNewFaction2.TaskGroups[0].Ships[1].Name = "Backup Battleship";
            oNewFaction2.TaskGroups[0].Ships[2].Name = "Crusher";
            foreach (KeyValuePair<OrdnanceDefTN, int> pair in oNewFaction2.ShipDesigns[1].ShipClassOrdnance)
            {
                oNewFaction2.TaskGroups[0].Ships[2].ShipOrdnance.Add(pair.Key, pair.Value);
            }
            oNewFaction2.TaskGroups[0].Ships[2].CurrentMagazineCapacity = oNewFaction2.ShipDesigns[1].PreferredOrdnanceSize;

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
