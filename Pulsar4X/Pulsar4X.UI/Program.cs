using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Pulsar4X.Stargen;
using log4net.Config;
using log4net;

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
            // gen Faction and a pop:
            Entities.Faction oNewFaction = new Entities.Faction();
            oNewFaction.Populations.Add(new Entities.Population(otest.Stars.FirstOrDefault().Planets.FirstOrDefault(), oNewFaction));
            oNewFaction.Populations.First().CivilianPopulation = 100.0f;
            GameState.Instance.Factions.Add(oNewFaction);

            Application.EnableVisualStyles();

            // Init our UI Controller:
            Helpers.UIController.Instance.Initialise();
            //Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Forms.MainForm());

            logger.Info("Program Ended");
        }
    }
}
