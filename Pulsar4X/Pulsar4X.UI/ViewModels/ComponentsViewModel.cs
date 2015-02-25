using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using System.ComponentModel;
using Pulsar4X.UI.Helpers;
using System.Linq.Expressions;

namespace Pulsar4X.UI.ViewModels
{
    public class ComponentsViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// List of all available components, so if we want to add to TN rules, change this here.
        /// </summary>
        public enum Components
        {
            ActiveMFC,
            BFC,
            CIWS,
            Cloak,
            EM,
            Engine,
            Gauss,
            Microwave,
            Jump,
            Laser,
            Magazine,
            Meson,
            MissileEngine,
            MissileLauncher,
            NewSpecies,
            Particle,
            Plasma,
            PlasmaTorp,
            Reactor,
            Rail,
            ShieldAbs,
            ShieldStd,
            Thermal,
            Count
        }


        /// <summary>
        /// global factions list.
        /// </summary>
        public BindingList<Faction> Factions { get; set; }

        /// <summary>
        /// List of all component strings.
        /// </summary>
        public BindingList<String> RPTechs { get; set; }

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

                _CurrnetComponent = Components.ActiveMFC;

                OnFactionChanged();
            }
        }

        /// <summary>
        /// The currently selected component tech project.
        /// </summary>
        private Components _CurrnetComponent;
        public Components CurrentComponent
        {
            get { return _CurrnetComponent; }
            set
            {
                _CurrnetComponent = value;

                if (_CurrnetComponent < 0 || _CurrnetComponent >= Components.Count)
                {
                    return;
                }

                OnComponentChanged();
            }
        }

        /// <summary>
        /// Constructor for the MV, Factions should already be set, and setting the current faction and component  should have those set when
        /// the panel is created.
        /// </summary>
        public ComponentsViewModel()
        {
            Factions = GameState.Instance.Factions;
            RPTechs = GameState.Instance.CompResearchTechs;

            if (Factions == null)
            {
                return;
            }

            _CurrnetFaction = Factions[0];
            _CurrnetComponent = Components.ActiveMFC;

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

        private void OnComponentChanged()
        {
            if (ComponentChanged != null)
            {
                ComponentChanged(this, new EventArgs());
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
        public event EventHandler ComponentChanged;


    }
}
