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
    public class FastOOBViewModel : INotifyPropertyChanged
    {


        /// <summary>
        /// Selected Faction
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
                    _TaskGroups = _currentFaction.TaskGroups;
                    OnFactionChanged();
                }
            }
        }

        /// <summary>
        /// Global factions list
        /// </summary>
        public BindingList<Faction> Factions { get; set; }

        /// <summary>
        /// Currently selected Ship class
        /// </summary>
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

        /// <summary>
        /// Faction list of designs.
        /// </summary>
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

        /// <summary>
        /// Currently selected taskgroup
        /// </summary>
        private TaskGroupTN _CurrnetTaskGroup;
        public TaskGroupTN CurrentTaskGroup
        {
            get { return _CurrnetTaskGroup; }
            set
            {
                if (_CurrnetTaskGroup != value)
                {
                    _CurrnetTaskGroup = value;
                    OnPropertyChanged(() => CurrentTaskGroup);
                }
            }
        }

        /// <summary>
        /// Faction list of Taskgroups.
        /// </summary>
        private BindingList<TaskGroupTN> _TaskGroups;
        public BindingList<TaskGroupTN> TaskGroups
        {
            get { return _TaskGroups; }
            set
            {
                if (_TaskGroups != value)
                {
                    _TaskGroups = value;
                    OnPropertyChanged(() => TaskGroups);
                    _CurrnetTaskGroup = _TaskGroups[0];
                }
            }
        }

        public FastOOBViewModel()
        {
            Factions = GameState.Instance.Factions;

            if (Factions == null)
            {
                return;
            }

            CurrentFaction = Factions.FirstOrDefault();

            if (CurrentFaction.TaskGroups.Count != 0)
            {
                CurrentTaskGroup = CurrentFaction.TaskGroups[0];
            }

            if (CurrentFaction.ShipDesigns.Count != 0)
            {
                CurrentShipClass = CurrentFaction.ShipDesigns[0];
            }
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

        private void OnTaskGroupChanged()
        {
            if (TaskGroupChanged != null)
            {
                TaskGroupChanged(this, new EventArgs());
            }
        }

        private void OnSpeciesChanged()
        {
            if (SpeciesChanged != null)
            {
                SpeciesChanged(this, new EventArgs());
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

        public event EventHandler SpeciesChanged;

        public event EventHandler TaskGroupChanged;

        public event EventHandler ShipClassChanged;

        
    }
}
