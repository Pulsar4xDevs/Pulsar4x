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
            IncPriority.CommandParameter = -1;
            DecPriority.CommandParameter = 1;
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
            //PercentComplete.Value = (int)viewModel.ItemPercentRemaining;
            //Completed.Text = viewModel.Completed.ToString();
        }

        public JobUC(JobVM<ColonyConstructionDB, ConstructionJob> viewModel) : this()
        {
            DataContext = viewModel;
            //PercentComplete.Value = (int)viewModel.ItemPercentRemaining;
            //Completed.Text = viewModel.Completed.ToString();
        }

    }
}
