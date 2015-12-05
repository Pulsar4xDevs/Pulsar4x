using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.AccessControl;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class JobAbilityView : Panel
    {
        protected Label PointsPerDay { get; set; }
        
        protected ListBox ItemJobs { get; set; }

        protected ComboBox ItemComboBox { get; set; }
        protected NumericUpDown NewJobBatchAmount { get; set; }
        protected CheckBox NewJobIsRepeated { get; set; }
        protected Button NewJobAdd { get; set; }

        public JobAbilityView()
        {
            XamlReader.Load(this);
        }

        public JobAbilityView(JobAbilityBaseVM<BaseDataBlob, object> viewModel)
        {
            PointsPerDay.Text = viewModel.PointsPerDay.ToString();

            ItemJobs.DataStore = new ObservableCollection<JobVM<BaseDataBlob,object>>(viewModel.ItemJobs);

            ItemComboBox.DataStore = new List<string>(viewModel.ItemDictionary.Keys);
            NewJobIsRepeated.Checked = viewModel.NewJobRepeat;
            NewJobAdd.Click += AddSelectedProjectOnClick;
            
        }

        private void AddSelectedProjectOnClick(object sender, EventArgs eventArgs)
        {
            throw new NotImplementedException();
        }
        
    }
}
