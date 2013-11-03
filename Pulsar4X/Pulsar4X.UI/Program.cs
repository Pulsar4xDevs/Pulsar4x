using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Pulsar4X.Stargen;
using log4net.Config;
using log4net;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.UI
{
    static class Program
    {

        public static readonly ILog logger = LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            XmlConfigurator.Configure(); // Enables Log4net based on App.config file.
            logger.Info("Program Started");

            // gen star system:
            Entities.StarSystem otest = GameState.Instance.StarSystemFactory.Create("Test");
            GameState.Instance.StarSystems.Add(otest);
            GameState.Instance.StarSystems.Add(GameState.Instance.StarSystemFactory.Create("Foo"));
            GameState.Instance.StarSystems.Add(GameState.Instance.StarSystemFactory.Create("Bar"));



            /// <summary>
            /// Alpha Build related area:
            /// </summary>


            // gen Faction and a pop:
            Entities.Faction oNewFaction = new Entities.Faction(0);

            /// <summary>
            /// This following section should probably be moved to the Faction constructor at some point.
            /// </summary>
            oNewFaction.Populations.Add(new Entities.Population(otest.Stars.FirstOrDefault().Planets.FirstOrDefault(), oNewFaction));
            oNewFaction.Populations.First().CivilianPopulation = 100.0f;
            GameState.Instance.Factions.Add(oNewFaction);
            oNewFaction.AddNewContactList(otest);

            Entities.Planet P1 = new Entities.Planet(otest.Stars[0]);
            P1.XSystem = 1.0;
            P1.YSystem = 1.0;

            oNewFaction.AddNewTaskGroup("Combat Taskgroup", P1, otest);
            oNewFaction.FactionColor = System.Drawing.Color.Blue;

            Entities.Faction oNewFaction2 = new Entities.Faction(1);
            oNewFaction2.Name = "Terran Federation";
            oNewFaction2.Populations.Add(new Entities.Population(otest.Stars[0].Planets[3], oNewFaction2));
            GameState.Instance.Factions.Add(oNewFaction2);
            oNewFaction2.AddNewContactList(otest);

            Entities.Planet P2 = new Entities.Planet(otest.Stars[0]);
            P2.XSystem = 1.05;
            P2.YSystem = 1.05;

            oNewFaction2.AddNewTaskGroup("Combat Taskgroup", P2, otest);
            oNewFaction2.FactionColor = System.Drawing.Color.Red;

            oNewFaction.AddNewShipDesign("Sword");
            ComponentDefListTN CL = oNewFaction.ComponentList;


            oNewFaction.ShipDesigns[0].AddEngine(CL.Engines[1], 8);
            oNewFaction.ShipDesigns[0].AddCrewQuarters(CL.CrewQuarters[0], 10);
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


            for (int loop = 1; loop < 8; loop++)
            {
                oNewFaction.ShipDesigns[0].AddBeamWeapon(CL.BeamWeaponDef[loop], 1);
            }
            oNewFaction.ShipDesigns[0].NewArmor("Composite",8,4);

            oNewFaction.TaskGroups[0].AddShip(oNewFaction.ShipDesigns[0], 0);
            oNewFaction.TaskGroups[0].AddShip(oNewFaction.ShipDesigns[0], 0);
            oNewFaction.TaskGroups[0].Ships[0].Refuel(1000000.0f);
            oNewFaction.TaskGroups[0].Ships[1].Refuel(1000000.0f);

            oNewFaction.TaskGroups[0].Ships[0].Name = "Warship";
            oNewFaction.TaskGroups[0].Ships[1].Name = "Backup Warship";

            oNewFaction2.AddNewShipDesign("Mace");
            CL = oNewFaction2.ComponentList;


            oNewFaction2.ShipDesigns[0].AddEngine(CL.Engines[1], 8);
            oNewFaction2.ShipDesigns[0].AddCrewQuarters(CL.CrewQuarters[0], 10);
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


            for (int loop = 1; loop < 8; loop++)
            {
                oNewFaction2.ShipDesigns[0].AddBeamWeapon(CL.BeamWeaponDef[loop], 1);
            }
            oNewFaction2.ShipDesigns[0].NewArmor("Composite", 8, 4);

            oNewFaction2.TaskGroups[0].AddShip(oNewFaction2.ShipDesigns[0], 0);
            oNewFaction2.TaskGroups[0].AddShip(oNewFaction2.ShipDesigns[0], 0);
            oNewFaction2.TaskGroups[0].Ships[0].Refuel(1000000.0f);
            oNewFaction2.TaskGroups[0].Ships[1].Refuel(1000000.0f);

            oNewFaction2.TaskGroups[0].Ships[0].Name = "Battleship";
            oNewFaction2.TaskGroups[0].Ships[1].Name = "Backup Battleship";

            oNewFaction.GiveAllTechs();
            //oNewFaction2.GiveAllTechs();

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

            logger.Info("Program Ended");
        }
    }
}
