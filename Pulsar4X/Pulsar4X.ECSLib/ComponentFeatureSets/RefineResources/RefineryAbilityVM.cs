using System;
using System.Windows.Input;

namespace Pulsar4X.ECSLib
{
    public class RefineryAbilityVM : JobAbilityBaseVM<RefiningDB, RefineingJob>
    {
        public RefineryAbilityVM(StaticDataStore staticData, Entity colonyEntity) : base(staticData, colonyEntity)
        {
            ItemDictionary = new DictionaryVM<Guid, string>(DisplayMode.Value);
            foreach (var kvp in _staticData_.ProcessedMaterials)
            {
                ItemDictionary.Add(kvp.Key, kvp.Value.Name);
            }
            //NewJobSelectedItem = ItemDictionary[ItemDictionary.ElementAt(0).Key];
            NewJobSelectedIndex = 0;
            NewJobBatchCount = 1;
            NewJobRepeat = false;
        }

        public override void OnNewBatchJob()
        {
            RefineingJob newjob = new RefineingJob(NewJobSelectedItem, NewJobBatchCount, _staticData_.ProcessedMaterials[NewJobSelectedItem].RefineryPointCost, NewJobRepeat);
            RefiningProcessor.AddJob(_staticData_, _colonyEntity_, newjob);
            Refresh();
        }
    }


    public class CommandHandler : ICommand
    {
        private Action _action;
        private bool _canExecute;
        public CommandHandler(Action action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action();
        }
    }

}