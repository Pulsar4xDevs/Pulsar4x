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
    public class JobPriorityCommand<TDataBlob, TJob> : ICommand
        where TDataBlob : BaseDataBlob
    {
        JobVM<TDataBlob, TJob> _jobVM { get; set; }

        public int DeltaUp { get; set; }
        public int DeltaDown { get; set; }

        public JobPriorityCommand(JobVM<TDataBlob, TJob> jobVM)
        {
            _jobVM = jobVM;
            DeltaUp = -1;
            DeltaDown = 1;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _jobVM.ChangePriority((int)parameter);
        }

        public event EventHandler CanExecuteChanged;
    }



}
