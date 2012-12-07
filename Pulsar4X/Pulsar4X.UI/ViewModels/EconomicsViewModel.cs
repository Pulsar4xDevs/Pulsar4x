using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Stargen;
using Pulsar4X.Entities;
using Pulsar4X.UI.Helpers;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Linq.Expressions;

namespace Pulsar4X.UI.ViewModels
{
    public class EconomicsViewModel : INotifyPropertyChanged
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
                _currentFaction = value;

                // safty check:
                if (_currentFaction == null)
                {
                    return;
                }

                Populations = _currentFaction.Populations;
            }
        }

        public BindingList<Faction> Factions { get; set; }

        private BindingList<Population> _populations;
        public BindingList<Population> Populations
        {
            get { return _populations; }
            set
            {
                _populations = value;
                OnPropertyChanged(() => Populations);
                CurrentPopulation = _populations[0];
            }
        }

        private Population _currentPopulation;
        public Population CurrentPopulation
        {
            get
            {
                return _currentPopulation;
            }
            set
            {
                _currentPopulation = value;
                OnPropertyChanged(() => CurrentPopulation);
            }
        }

        #endregion

        public EconomicsViewModel()
        {
            Factions = GameState.Instance.Factions;

            if (Factions == null)
            {
                return;
            }

            CurrentFaction = Factions.FirstOrDefault();

            if (Populations != null)
            {
                CurrentPopulation = Populations.FirstOrDefault();
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
    }
}
