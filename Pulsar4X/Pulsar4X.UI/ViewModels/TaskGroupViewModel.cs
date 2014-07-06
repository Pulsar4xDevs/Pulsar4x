using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.Entities;
using System.ComponentModel;
using Pulsar4X.Stargen;
using Pulsar4X.UI.Helpers;
using System.Linq.Expressions;

namespace Pulsar4X.UI.ViewModels
{
    public class TaskGroupViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// List of all Factions.
        /// </summary>
        public BindingList<Faction> Factions { get; set; }

        /// <summary>
        /// Current Faction.
        /// </summary>
        private Faction _CurrentFaction;
        public Faction CurrentFaction
        {
            get { return _CurrentFaction; }
            set
            {
                if (_CurrentFaction != value)
                {
                    _CurrentFaction = value;
                    OnFactionChanged();
                }
            }
        }

        /// <summary>
        /// All the current Faction's Taskgroups
        /// </summary>
        public BindingList<TaskGroupTN> TaskGroups { get; set; }

        /// <summary>
        /// Currently Selected TaskGroup
        /// </summary>
        private TaskGroupTN _CurrentTaskGroup;
        public TaskGroupTN CurrentTaskGroup
        {
            get { return _CurrentTaskGroup; }
            set 
            { 
                if(_CurrentTaskGroup != value)
                {
                    _CurrentTaskGroup = value;
                    OnTaskGroupChanged();
                }
            }
        }

        /// <summary>
        /// Constructor for TGViewModel. Initialization is to faction 0 task group 0.
        /// Later functionality will handle updating faction and taskgroup indices.
        /// </summary>
        public TaskGroupViewModel()
        {
            _CurrentFaction = GameState.Instance.Factions[0];
            Factions = GameState.Instance.Factions;

            if(GameState.Instance.Factions[0].TaskGroups.Count != 0)
                _CurrentTaskGroup = GameState.Instance.Factions[0].TaskGroups[0];

            TaskGroups = GameState.Instance.Factions[0].TaskGroups;
        }


        private void OnFactionChanged()
        {
            TaskGroups = GameState.Instance.Factions[_CurrentFaction.FactionID].TaskGroups;

            if(TaskGroups.Count != 0)
                _CurrentTaskGroup = TaskGroups[0];


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
        public event EventHandler TaskGroupChanged;
    }
}
