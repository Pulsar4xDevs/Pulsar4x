using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;

namespace Pulsar4X.ViewModel
{

    /// <summary>
    /// This view model maps to the main game class. It provides lists of factions, systems and other high level info.
    /// </summary>
    public class GameVM : IViewModel
    {
        private ObservableCollection<SystemVM> _systems;


        public TimeControlVM TimeControl { get; } = new TimeControlVM();


        public Player CurrentPlayer { get; private set; }

        public AuthenticationToken CurrentAuthToken { get; private set; }

        internal Entity CurrentFaction
        {
            get { return _currentFaction; }
            set
            {
                _currentFaction = value;
                //TODO: factionDB.knownfactions need to be filled with... a blank copy of the actual faction that gets filled as the facion finds out more about it?
                //excepting in the case of GM where the actual faction should be good. 
                _visibleFactions = new List<Guid>();
                foreach (var knownFaction in _currentFaction.GetDataBlob<FactionInfoDB>().KnownFactions)
                {
                    _visibleFactions.Add(knownFaction.Guid);
                }
                _systems.Clear();
                _systemDictionary.Clear();
                foreach (var knownsystem in _currentFaction.GetDataBlob<FactionInfoDB>().KnownSystems)
                {
                    SystemVM systemVM = SystemVM.Create(this, knownsystem);
                    _systems.Add(systemVM);
                    _systemDictionary.Add(systemVM.ID, systemVM);
                }

                Colonys.Clear();
                foreach (var colonyEntity in _currentFaction.GetDataBlob<FactionInfoDB>().Colonies)
                {
                    Colonys.Add(colonyEntity.Guid, colonyEntity.GetDataBlob<NameDB>().GetName(_currentFaction));
                }
                Colonys.SelectedIndex = 0;

                OnPropertyChanged();
            }
        }
        private Entity _currentFaction;



        public string StatusText { get { return _statusText; } set { _statusText = value; OnPropertyChanged(); } }
        private string _statusText;

        public SystemView.StarSystemVM StarSystemViewModel {get; private set;}

        //factions that this client has full visability of. for GM this will be all factions.
        private List<Guid> _visibleFactions;

        //faction data. for GM this will be compleate, for normal play this will be factions known to the faction, and the factionVM will only contain data that is known to the faction
        private BindingList<FactionVM> _factions;

        public ObservableCollection<SystemVM> StarSystems { get { return _systems; } }

        public DictionaryVM<Guid, string> Colonys { get; } = new DictionaryVM<Guid, string>(DisplayMode.Value);

        public ColonyScreenVM SelectedColonyScreenVM { get { return new ColonyScreenVM(this, Game.GlobalManager.GetGlobalEntityByGuid(Colonys.SelectedKey), Game.StaticData); } }


        private Dictionary<Guid, SystemVM> _systemDictionary;

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
                CreatePlayerFaction =  options.CreatePlayerFaction,
                DefaultFactionName = options.FactionName,
                DefaultPlayerPassword = options.FactionPassword,
                DefaultSolStart = options.DefaultStart,
            };

            Game = new Game(gameSettings);
            
            // TODO: Select default player more reliably
            CurrentPlayer = Game.Players[0];
            CurrentAuthToken = new AuthenticationToken(CurrentPlayer, options.FactionPassword);

            ReadOnlyDictionary<Entity, AccessRole> roles = CurrentPlayer.GetAccessRoles(CurrentAuthToken);

            CurrentFaction = roles.FirstOrDefault(role => (role.Value & AccessRole.Owner) != 0).Key;


            StatusText = "Game Created.";

            StarSystemViewModel = new SystemView.StarSystemVM(this, Game, CurrentFaction);
            StarSystemViewModel.Initialise();
        }

        public void LoadGame(string pathToFile)
        {
            StatusText = "Loading Game...";
            Game = SerializationManager.ImportGame(pathToFile);


            // TODO: Select Default player, generate auth token for them.
            CurrentAuthToken = new AuthenticationToken(Game.SpaceMaster);
            CurrentFaction = Game.GameMasterFaction;

            StatusText = "Game Loaded.";
        }

        public void SaveGame(string pathToFile)
        {
            StatusText = "Saving Game...";

            SerializationManager.Export(Game, pathToFile);

            StatusText = "Game Saved";
        }

        public void SetPlayer(Player player, string password)
        {
            CurrentPlayer = player;
            CurrentAuthToken = new AuthenticationToken(player, password);

            ReadOnlyDictionary<Entity, AccessRole> playerAccessRoles = player.GetAccessRoles(CurrentAuthToken);
            foreach (KeyValuePair<Entity, AccessRole> kvp in playerAccessRoles)
            {
                Entity faction = kvp.Key;
                AccessRole role = kvp.Value;

                // For now, just select the first faction with the Owner role.
                if ((role & AccessRole.Owner) == AccessRole.Owner)
                {
                    CurrentFaction = faction;
                    break;
                }
            }
        }
        

        private Game _game;
        private Player _currentPlayer;

        internal Game Game
        {
            get
            {
                return _game;
            }
            set
            {
                _game = value;
                OnPropertyChanged("HasGame");
                TimeControl.Initialise(this);
                
                //forces anything listing for a change in the HasGame property to update. 
                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HasGame"));
            }
        }

        /// <summary>
        /// returns true if a game has been created, loaded etc. 
        /// </summary>
        public bool HasGame => Game != null;


        public GameVM()
        {
            _systems = new ObservableCollection<SystemVM>();
            _systemDictionary = new Dictionary<Guid, SystemVM>();
        }

        #region IViewModel

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh(bool partialRefresh = false)
        {
            foreach (var system in _systems)
            {
                system.Refresh();
            }
        }

        #endregion
    }
}