using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ECSLib
{
    public class GameVM : ViewModelBase
    {

        Entity _currentFactionEntity;

        internal Game Game { get; set; }
        public bool HasGame => Game != null;
        
        public string CurrentFactionName { get; set; } = "";
        public string StatusText { get; set; } = "";

        public List<EntityVM> ViewedEntites { get; set; } = new List<EntityVM>(); 

        
        ObservableCollection<SystemVM> _systems = new ObservableCollection<SystemVM>();
        Dictionary<Guid, SystemVM> _systemDictionary = new Dictionary<Guid, SystemVM>();
        
        public GameVM()
        {
            _systems = new ObservableCollection<SystemVM>();
            _systemDictionary = new Dictionary<Guid, SystemVM>();
        }
        public void Refresh(bool partialRefresh = false)
        {
            foreach (var system in _systems)
            {
                system.Refresh();
            }
        }

        public SystemVM GetSystem(Guid bodyGuid)
        {
            Entity bodyEntity;
            Guid rootGuid = new Guid();
            if (_systemDictionary.ContainsKey(bodyGuid))
                rootGuid = bodyGuid;

            else if (Game.GlobalManager.FindEntityByGuid(bodyGuid, out bodyEntity))
            {
                if (bodyEntity.HasDataBlob<OrbitDB>())
                {
                    rootGuid = bodyEntity.GetDataBlob<OrbitDB>().ParentDB.Root.Guid;
                }
            }
            else throw new GuidNotFoundException(bodyGuid);

            if (!_systemDictionary.ContainsKey(rootGuid))
            {
                SystemVM systemVM = SystemVM.Create(this, rootGuid);
                _systems.Add(systemVM);
                _systemDictionary.Add(rootGuid, systemVM);
            }
            return _systemDictionary[rootGuid];
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
