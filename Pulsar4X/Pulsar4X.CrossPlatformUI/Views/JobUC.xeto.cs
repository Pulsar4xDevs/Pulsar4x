using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class JobUC : Panel
    {
        protected Label Item { get; set; }
        protected ProgressBar PercentComplete { get; set; }
        protected Label Completed { get; set; }
        protected NumericUpDown BatchQuantity { get; set; }
        protected CheckBox RepeatJob { get; set; }
        protected Button IncPriority { get; set; }
        protected Button DecPriority { get; set; }

        public JobUC()
        {
            XamlReader.Load(this);
        }

        //public JobUC(JobVM<BaseDataBlob, object> viewModel) : this()
        //{
        //    Item.Text = viewModel.Item;
        //    PercentComplete.Value = (int)viewModel.ItemPercentRemaining;
        //    Completed.Text = viewModel.Completed.ToString();
        //    BatchQuantity.Value = viewModel.BatchQuantity;
        //    RepeatJob.Checked = viewModel.Repeat;
        //}


        public JobUC(JobVM<ColonyRefiningDB, RefineingJob> viewModel) : this()
        {
            DataContext = viewModel;
            IncPriority.Command = viewModel.JobPriorityCommand;
            IncPriority.CommandParameter = viewModel.JobPriorityCommand.DeltaDown;
            DecPriority.Command = viewModel.JobPriorityCommand;
            DecPriority.CommandParameter = viewModel.JobPriorityCommand.DeltaUp;
            //PercentComplete.Value = (int)viewModel.ItemPercentRemaining;
            //Completed.Text = viewModel.Completed.ToString();


        }

        public JobUC(JobVM<ColonyConstructionDB, ConstructionJob> viewModel) : this()
        {
            DataContext = viewModel;
            IncPriority.Command = viewModel.JobPriorityCommand;
            IncPriority.CommandParameter = viewModel.JobPriorityCommand.DeltaUp;
            DecPriority.Command = viewModel.JobPriorityCommand;
            DecPriority.CommandParameter = viewModel.JobPriorityCommand.DeltaDown;
            //PercentComplete.Value = (int)viewModel.ItemPercentRemaining;
            //Completed.Text = viewModel.Completed.ToString();


        }

    }
}
