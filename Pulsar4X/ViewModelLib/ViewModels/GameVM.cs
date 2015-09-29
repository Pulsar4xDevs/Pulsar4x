using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI.ViewModels
{
    /// <summary>
    /// This view model maps to the main game class. It porivdes lists of factions, systems and other high level info.
    /// </summary>
    public class GameVM : IViewModel
    {
        private BindingList<SystemVM> _systems;

        private Entity _playerFaction;
        public Entity PlayerFaction { get{return _playerFaction;}
            set
            {
                _playerFaction = value;
                //TODO: factionDB.knownfactions need to be filled with... a blank copy of the actual faction that gets filled as the facion finds out more about it?
                //excepting in the case of GM where the actual faction should be good. 
                _visibleFactions = new List<Guid>();
                foreach (var knownFaction in _playerFaction.GetDataBlob<FactionInfoDB>().KnownFactions)
                {
                    _visibleFactions.Add(knownFaction.Guid);
                }
                _systems = new BindingList<SystemVM>();
                _systemDictionary = new Dictionary<Guid, SystemVM>();
                foreach (var knownsystem in _playerFaction.GetDataBlob<FactionInfoDB>().KnownSystems)
                {
                    SystemVM systemVM = SystemVM.Create(this, knownsystem);
                    _systems.Add(systemVM);
                    _systemDictionary.Add(systemVM.ID, systemVM);
                }
            } 
        }

        //factions that this client has full visability of. for GM this will be all factions.
        private List<Guid> _visibleFactions; 

        //faction data. for GM this will be compleate, for normal play this will be factions known to the faction, and the factionVM will only contain data that is known to the faction
        private BindingList<FactionVM> _factions; 


        public BindingList<SystemVM> StarSystems { get { return _systems; } }

        
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
                _systemDictionary.Add(rootGuid,systemVM);
            }
            return _systemDictionary[rootGuid];
        }


        internal Game Game { get; private set; }

        public GameVM(Game game)
        {
            Game = game;
            _systems = new BindingList<SystemVM>();
            _systemDictionary = new Dictionary<Guid, SystemVM>();
            PlayerFaction = game.GameMasterFaction; //on creation the player faction can be set to GM I guess... for now anyway.
        }

        #region IViewModel

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
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