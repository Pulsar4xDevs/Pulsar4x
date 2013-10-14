using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Pulsar4X.Lib;
using Pulsar4X.UI.Helpers;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Linq.Expressions;

namespace Pulsar4X.UI.ViewModels
{
    public class ShipsViewModel : INotifyPropertyChanged
    {

        #region Properties

        public BindingList<Faction> Factions { get; set; }

        private BindingList<ShipTN> _ships; 
        public BindingList<ShipTN> Ships
        {
            get
            {
                return _ships;
            }
            set
            {
                _ships = value;
            }
        }

        private Faction _currentFaction;
        public Faction CurrentFaction
        {
            get
            {
                return _currentFaction;
            }
            set
            {
                _currentFaction = value;

                // safty check:
                if (_currentFaction == null)
                {
                    return;
                }

                Ships = _currentFaction.Ships;
                OnFactionChanged();
            }
        }


        #endregion


        public ShipsViewModel()
        {
            Factions = GameState.Instance.Factions;

            if (Factions == null)
            {
                return;
            }

            CurrentFaction = Factions[0];   
        }

        private void OnFactionChanged()
        {
            if (FactionChanged != null)
            {
                FactionChanged(this, new EventArgs());
            }
        }

        private void OnPropertyChanged(Expression<Func<object>> property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this,
                    new PropertyChangedEventArgs(BindingHelper.Name(property)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler FactionChanged;
        public event EventHandler PopulationChanged;
    }
}
