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
using Pulsar4X.Entities.Components;

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

        /// <summary>
        /// Currently selected faction.
        /// </summary>
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

        /// <summary>
        /// Currently Selected ship.
        /// </summary>
        private ShipTN _currentShip;
        public ShipTN CurrentShip
        {
            get { return _currentShip; }

            set
            {
                _currentShip = value;

                if (_currentShip == null)
                {
                    return;
                }

                FireControls = _currentShip.ShipFireControls;

                OnShipChanged();
            }
        }

        /// <summary>
        /// List of Ship Firecontrols.
        /// </summary>
        public BindingList<ComponentTN> FireControls { get; set; }

        private ComponentTN _currentFC;
        public ComponentTN currentFC
        {
            get { return _currentFC; }

            set
            {
                _currentFC = value;

                if (_currentFC == null)
                {
                    return;
                }

                OnSFCChanged();
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

        private void OnShipChanged()
        {
            if(ShipChanged != null)
            {
                ShipChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// I don't really know anything about this, just cargo culting here. the windows event system needs this?
        /// </summary>
        private void OnSFCChanged()
        {
            if(SFCChanged != null)
            {
                SFCChanged(this, new EventArgs());
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
        public event EventHandler SFCChanged;
        public event EventHandler ShipChanged;
        public event EventHandler PopulationChanged;
    }
}
