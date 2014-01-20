using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;
using System.ComponentModel;
using Pulsar4X.Stargen;
using Pulsar4X.UI.Helpers;
using System.Linq.Expressions;

/// <summary>
/// Don't forget to put the .UI. in here
/// </summary>
namespace Pulsar4X.UI.ViewModels
{
    /// <summary>
    /// INotifyPropertyChanged had better be there.
    /// </summary>
    public class MissileDesignViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// List of all Factions.
        /// </summary>
        public BindingList<Faction> Factions { get; set; }

        /// <summary>
        /// Current Faction.
        /// </summary>
        private Faction _CurrnetFaction;
        public Faction CurrentFaction
        {
            get { return _CurrnetFaction; }
            set
            {
                if (_CurrnetFaction != value)
                {
                    _CurrnetFaction = value;
                    OnFactionChanged();
                }
            }
        }

        /// <summary>
        /// List of all this factions missile engines.
        /// </summary>
        public BindingList<MissileEngineDefTN> MissileEngines { get; set; }

        /// <summary>
        /// Current Missile Engine
        /// </summary>
        private MissileEngineDefTN _CurrnetMissileEngine;
        public MissileEngineDefTN CurrentMissileEngine
        {
            get { return _CurrnetMissileEngine; }
            set
            {
                if (_CurrnetMissileEngine != value)
                {
                    _CurrnetMissileEngine = value;
                    OnMissileEngineChanged();
                }
            }
        }

        /// <summary>
        /// Constructor for this view model.
        /// </summary>
        public MissileDesignViewModel()
        {
            _CurrnetFaction = GameState.Instance.Factions[0];
            Factions = GameState.Instance.Factions;

            MissileEngines = _CurrnetFaction.ComponentList.MissileEngineDef;

            if (MissileEngines.Count != 0)
                CurrentMissileEngine = MissileEngines[0];
        }

        /// <summary>
        /// when the faction is changed this will be run.
        /// </summary>
        private void OnFactionChanged()
        {
            MissileEngines = _CurrnetFaction.ComponentList.MissileEngineDef;

            if(MissileEngines.Count != 0)
                CurrentMissileEngine = MissileEngines[0];

            if (FactionChanged != null)
            {
                FactionChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// need this as well.
        /// </summary>
        private void OnMissileEngineChanged()
        {
            if (MissileEngineChanged != null)
            {
                MissileEngineChanged(this, new EventArgs());
            }
        }

        /// <summary>
        /// On property changed has to be defined for all of these.
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
        public event EventHandler MissileEngineChanged;

    }
}
