using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Lib;
using Pulsar4X.Entities;
using Pulsar4X.UI.Helpers;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Linq.Expressions;


namespace Pulsar4X.UI.ViewModels
{
    public class ClassDesignViewModel : INotifyPropertyChanged
    {
        #region Properties

        private Faction _currentFaction;
        public Faction CurrentFaction
        {
            get
            {
                return _currentFaction;
            }
            set
            {
                if (_currentFaction != value)
                {
                    _currentFaction = value;

                    // safty check:
                    if (_currentFaction == null)
                    {
                        return;
                    }

                    OnPropertyChanged(() => CurrentFaction);
                    _ShipDesigns = _currentFaction.ShipDesigns;
                    OnFactionChanged();
                }
            }
        }

        public BindingList<Faction> Factions { get; set; }

        private ShipClassTN _CurrentShipClass;
        public ShipClassTN CurrentShipClass
        {
            get
            {
                return _CurrentShipClass;
            }
            set
            {
                if (_CurrentShipClass != value)
                {
                    _CurrentShipClass = value;

                    OnPropertyChanged(() => CurrentShipClass);
                    OnShipClassChanged();
                }
            }
        }

        private BindingList<ShipClassTN> _ShipDesigns;
        public BindingList<ShipClassTN> ShipDesigns
        {
            get
            {
                return _ShipDesigns;
            }
            set
            {
                if (_ShipDesigns != value)
                {
                    _ShipDesigns = value;
                    OnPropertyChanged(() => ShipDesigns);
                    _CurrentShipClass = _ShipDesigns[0];
                }
            }
        }


        //private ShipClassTN _CurrentClassType;
        //public ShipClassTN CurrentClassType
        //{
        //    get
        //    {
        //        return _CurrentClassType;
        //    }
        //    set
        //    {
        //        _CurrentClassType = value;
        //        OnPropertyChanged(() => CurrentClassType);
        //        //OnShipClassChanged();
        //    }
        //}

        //private BindingList<ShipClassTN> _ClassTypes;
        //public BindingList<ShipClassTN> ClassTypes
        //{
        //    get
        //    {
        //        return _ClassTypes;
        //    }
        //    set
        //    {
        //        _ClassTypes = value;
        //        OnPropertyChanged(() => ClassTypes);
        //        _CurrentClassType = _ClassTypes[0];
        //    }
        //}


        #endregion


        public ClassDesignViewModel()
        {
            Factions = GameState.Instance.Factions;

            if (Factions == null)
            {
                return;
            }

            CurrentFaction = Factions.FirstOrDefault();
        }

        private void OnShipClassChanged()
        {
            if (ShipClassChanged != null)
            {
                ShipClassChanged(this, new EventArgs());
            }
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

        public event EventHandler ShipClassChanged;

        public event EventHandler FactionChanged;
    }
}
