using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class JobIncreasePriorityCommand : ICommand
    {


        public JobIncreasePriorityCommand()
        {
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            JobVM job = (JobVM)parameter;
            job.ChangePriority(-1);
        }

        public event EventHandler CanExecuteChanged;
    }

    public class JobDecreasePriorityCommand : ICommand
    {


        public JobDecreasePriorityCommand()
        {

        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            JobVM job = (JobVM)parameter;
            job.ChangePriority(1);
        }

        public event EventHandler CanExecuteChanged;
    }

}
