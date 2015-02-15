using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using System.ComponentModel;
using Pulsar4X.UI.Helpers;
using System.Linq.Expressions;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.UI.ViewModels
{
    public class TurretDesignViewModel
    {
        /// <summary>
        /// global factions list.
        /// </summary>
        public BindingList<Faction> Factions { get; set; }

        /// <summary>
        /// Currently selected faction
        /// </summary>
        private Faction _CurrnetFaction;
        public Faction CurrentFaction
        {
            get { return _CurrnetFaction; }
            set
            {
                _CurrnetFaction = value;

                if (_CurrnetFaction == null)
                {
                    return;
                }

                OnFactionChanged();
            }
        }


        /// <summary>
        /// global list of beams that can be turreted.
        /// </summary>
        public BindingList<BeamDefTN> TurretableList { get; set; }

        /// <summary>
        /// Currently selected faction
        /// </summary>
        private BeamDefTN _CurrnetBeam;
        public BeamDefTN CurrentBeam
        {
            get { return _CurrnetBeam; }
            set
            {
                _CurrnetBeam = value;

                if (_CurrnetBeam == null)
                {
                    return;
                }

                OnBeamChanged();
            }
        }

        /// <summary>
        /// initialize Factions to the gamestate faction list, and turretable list to faction[0]'s list of turretables.
        /// </summary>
        public TurretDesignViewModel()
        {
            Factions = GameState.Instance.Factions;

            if (Factions == null)
                return;

            CurrentFaction = Factions[0];
            TurretableList = CurrentFaction.ComponentList.TurretableBeamDef;

            if (TurretableList.Count != 0)
                CurrentBeam = TurretableList[0];
        }

        /// <summary>
        /// Event handling!
        /// </summary>
        private void OnFactionChanged()
        {
            if (FactionChanged != null)
            {
                FactionChanged(this, new EventArgs());
            }
        }

        private void OnBeamChanged()
        {
            if (BeamChanged != null)
            {
                BeamChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// This is needed for INotifyPropertyChanged, all I know about it.
        /// </summary>
        /// <param name="property"></param>
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
        public event EventHandler BeamChanged;
    }
}
