using System;
using System.Collections.Generic;
using System.ComponentModel;
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

         

        private BindingList<SystemVM> _factions;


        internal BindingList<SystemVM> StarSystems { get { return _systems; } }

        
        private Dictionary<Guid, SystemVM> _systemDictionary;

        internal SystemVM GetSystem(Guid bodyGuid)
        {
            Entity bodyEntity;
            Guid rootGuid = new Guid();
            if (_systemDictionary.ContainsKey(bodyGuid))
                rootGuid = bodyGuid;
            
            else if (_game.GlobalManager.FindEntityByGuid(bodyGuid, out bodyEntity))
            {                
                if (bodyEntity.HasDataBlob<OrbitDB>())
                {
                     rootGuid = bodyEntity.GetDataBlob<OrbitDB>().ParentDB.Root.Guid;
                }
            }
            else throw new GuidNotFoundException(bodyGuid);

            if (!_systemDictionary.ContainsKey(rootGuid))
            {
                SystemVM systemVM = SystemVM.Create(rootGuid);
                _systems.Add(systemVM);
                _systemDictionary.Add(rootGuid,systemVM);
            }
            return _systemDictionary[rootGuid];
        }


        private Game _game;

        public GameVM(Game game)
        {
            _game = game;
            _systems = new BindingList<SystemVM>();
            _systemDictionary = new Dictionary<Guid, SystemVM>();
            foreach (var system in game.Systems)
            {
                SystemVM systemVM = SystemVM.Create(system);
                _systems.Add(systemVM);
                _systemDictionary.Add(systemVM.ID, systemVM);
            }
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
            throw new NotImplementedException();
        }

        #endregion
    }
}