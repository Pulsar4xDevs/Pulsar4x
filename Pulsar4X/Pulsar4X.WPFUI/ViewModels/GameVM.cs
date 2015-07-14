using System;
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


        private BindingList<SystemVM> StarSystems;


        private Game _game;


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