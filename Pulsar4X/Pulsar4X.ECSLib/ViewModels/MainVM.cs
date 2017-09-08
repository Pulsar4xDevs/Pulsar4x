using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class MainVM
    {

        Entity _currentFactionEntity;
        Game Game { get; set; }

        public string CurrentFactionName { get; set; } = "";
        public string StatusText { get; set; } = "";

        public List<EntityVM> ViewedEntites { get; set; } = new List<EntityVM>(); 

        public MainVM()
        {
        }




        public void CreateGame(NewGameOptionsVM options)
        {
            StatusText = "Creating Game...";

            // TODO: Databind the GameSettings object in the NewGameOptionsVM
            var gameSettings = new NewGameSettings
            {
                GameName = "Test Game",
                MaxSystems = options.NumberOfSystems,
                SMPassword = options.GmPassword,
                DataSets = options.SelectedModList.Select(dvi => dvi.Directory),
                CreatePlayerFaction = options.CreatePlayerFaction,
                DefaultFactionName = options.FactionName,
                DefaultPlayerPassword = options.FactionPassword,
                DefaultSolStart = options.DefaultStart,
            };

            Game = new Game(gameSettings);

            //_currentFactionEntity = roles.FirstOrDefault(role => (role.Value & AccessRole.Owner) != 0).Key;


            StatusText = "Game Created.";

            //StarSystemViewModel = new SystemView.StarSystemVM(this, Game, _currentFactionEntity);
            //StarSystemViewModel.Initialise();
        }

        public void LoadGame(string pathToFile)
        {
            StatusText = "Loading Game...";
            Game = SerializationManager.ImportGame(pathToFile);


            // TODO: Select Default player, generate auth token for them.
            //CurrentAuthToken = new AuthenticationToken(Game.SpaceMaster);
            _currentFactionEntity = Game.GameMasterFaction;

            StatusText = "Game Loaded.";
        }

    }
}
