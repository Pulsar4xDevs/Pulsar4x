using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Pulsar4X.ViewModel
{
    public class JobPriorityCommand : ICommand
    {
        public JobVM JobVM { get; set; }

        public JobPriorityCommand(JobVM viewModel)
        {
            JobVM = viewModel;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            JobVM.ChangePriority((string)parameter);
        }

        public event EventHandler CanExecuteChanged;
    }
}
