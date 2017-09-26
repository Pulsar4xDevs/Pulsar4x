using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib;

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
            IncPriority.CommandParameter = (short)-1;
            DecPriority.CommandParameter = (short)1;
        }

        //public JobUC(JobVM<BaseDataBlob, object> viewModel) : this()
        //{
        //    Item.Text = viewModel.Item;
        //    PercentComplete.Value = (int)viewModel.ItemPercentRemaining;
        //    Completed.Text = viewModel.Completed.ToString();
        //    BatchQuantity.Value = viewModel.BatchQuantity;
        //    RepeatJob.Checked = viewModel.Repeat;
        //}


        public JobUC(JobVM<RefiningDB, RefineingJob> viewModel) : this()
        {
            DataContext = viewModel;
            //PercentComplete.Value = (int)viewModel.ItemPercentRemaining;
            //Completed.Text = viewModel.Completed.ToString();
        }

        public JobUC(JobVM<ConstructionDB, ConstructionJob> viewModel) : this()
        {
            DataContext = viewModel;
            //PercentComplete.Value = (int)viewModel.ItemPercentRemaining;
            //Completed.Text = viewModel.Completed.ToString();
        }

    }
}
