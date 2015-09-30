using System;
using System.Threading;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModels
{
    public class NewGameOptionsVM
    {
        public delegate void ProgressUpdate(double progress);

        public double StatusValue;

        public string GmPassword { get; set; }
        
        public bool CreatePlayerFaction { get; set; }
        public string FactionName { get; set; }
        public string FactionPassword { get; set; }
        public bool DefaultStart { get; set; }

        public int NumberOfSystems { get; set; }

        public async void CreateGame()
        {
            Game newGame =  await Task.Run(() => Game.NewGame("Test Game", new DateTime(2050, 1, 1), NumberOfSystems, new Progress<double>(OnProgressUpdate)));
            if (CreatePlayerFaction && DefaultStart)
            {
                StarSystemFactory starfac = new StarSystemFactory(newGame);
                StarSystem sol = starfac.CreateSol(newGame);
                Entity earth = sol.SystemManager.Entities[3]; //should be fourth entity created 
                Entity factionEntity = FactionFactory.CreateFaction(newGame, FactionName);
                Entity speciesEntity = SpeciesFactory.CreateSpeciesHuman(factionEntity, newGame.GlobalManager);
                Entity colonyEntity = ColonyFactory.CreateColony(factionEntity, speciesEntity, earth);
                colonyEntity.GetDataBlob<ColonyInfoDB>().Population[speciesEntity] = 9000000000;
            }
        }

        /// <summary>
        /// OnProgressUpdate eventhandler for the Progress class.
        /// Called from the task thread, this call must be marshalled to the UI thread.
        /// </summary>
        private void OnProgressUpdate(double progress)
        {
            // The Dispatcher contains the UI thread. Make sure we are on the UI thread.
            //if (Thread.CurrentThread != Dispatcher.Thread)
            //{
            //    Dispatcher.BeginInvoke(new ProgressUpdate(OnProgressUpdate), progress);
            //    return;
            //}

            StatusValue = progress * 100;
        }

    }
}
