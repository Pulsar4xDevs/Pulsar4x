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

        public Player CurrentPlayer { get; set; }
        public AuthenticationToken CurrentAuthToken { get; set; }

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
                ColonyScreens = new List<ColonyScreenVM>();
                Colonys = new DictionaryVM<Guid, string>(DisplayMode.Value);
                foreach (var colonyEntity in _currentFaction.GetDataBlob<FactionInfoDB>().Colonies)
                {
                    ColonyScreens.Add(new ColonyScreenVM(colonyEntity, Game.StaticData));
                    Colonys.Add(colonyEntity.Guid, colonyEntity.GetDataBlob<NameDB>().GetName(_currentFaction));
                }


            }
        }
        private Entity _currentFaction;

        //progress bar in MainWindow should be bound to this
        public double ProgressValue
        {
            get { return _progressValue; }
            set
            {
                _progressValue = value;
                OnPropertyChanged();
            }
        }
        private double _progressValue;

        public string StatusText { get { return _statusText; } set { _statusText = value; OnPropertyChanged(); } }
        private string _statusText;



        //factions that this client has full visability of. for GM this will be all factions.
        private List<Guid> _visibleFactions;

        //faction data. for GM this will be compleate, for normal play this will be factions known to the faction, and the factionVM will only contain data that is known to the faction
        private BindingList<FactionVM> _factions;

        public ObservableCollection<SystemVM> StarSystems { get { return _systems; } }

        public List<ColonyScreenVM> ColonyScreens { get; set; } //TODO create the VM as a view is requested?

        public DictionaryVM<Guid, string> Colonys { get; set; }

        public ColonyScreenVM ColonyScreen { get; set; }

        private Guid _selectedColonyGuid;
        public Guid SetColonyScreen
        {
            get { return _selectedColonyGuid; }
            set
            {
                _selectedColonyGuid = value; ColonyScreen = new ColonyScreenVM(Game.GlobalManager.GetEntityByGuid(value), Game.StaticData);
            }
        }

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

            ProgressValue = 0;//reset the progressbar
            StatusText = "Game Created.";
        }

        public void LoadGame(string pathToFile)
        {
            StatusText = "Loading Game...";
            Game = SerializationManager.ImportGame(pathToFile);


            // TODO: Select Default player, generate auth token for them.
            CurrentAuthToken = new AuthenticationToken(Game.SpaceMaster);
            CurrentFaction = Game.GameMasterFaction;

            ProgressValue = 0;
            StatusText = "Game Loaded.";
        }

        public void SaveGame(string pathToFile)
        {
            StatusText = "Saving Game...";

            SerializationManager.Export(Game, pathToFile);
            ProgressValue = 0;
            StatusText = "Game Saved";
        }

        

        public void AdvanceTime(TimeSpan pulseLength, CancellationToken pulseCancellationToken)
        {
            var pulseProgress = new Progress<double>(UpdatePulseProgress);
            
            int secondsPulsed;

            secondsPulsed = Game.AdvanceTime((int)pulseLength.TotalSeconds, pulseCancellationToken, pulseProgress);
            Refresh();
            //e.Handled = true;
            ProgressValue = 0;
        }

        public ICommand AdvanceTimeCmd { get { return new RelayCommand<string>(param => AdvanceTime(param)); } }
        public void AdvanceTime(string seconds)
        {
            var pulseProgress = new Progress<double>(UpdatePulseProgress);
            int secondsPulsed;
            secondsPulsed = Game.AdvanceTime(int.Parse(seconds), pulseProgress);
            Refresh();
               
        }

        private void UpdatePulseProgress(double progress)
        {
            // Do some UI stuff with Progress percent
            ProgressValue = progress * 100;
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

            ProgressValue = progress * 100;
        }

        private Game _game;

        internal Game Game
        {
            get
            {
                return _game;
            }
            set
            {
                _game = value;
                OnPropertyChanged();

                //forces anything listing for a change in the HasGame property to update. 
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HasGame"));
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
            foreach (var colonyVM in ColonyScreens)
            {
                colonyVM.Refresh();
            }
        }

        #endregion
    }
}