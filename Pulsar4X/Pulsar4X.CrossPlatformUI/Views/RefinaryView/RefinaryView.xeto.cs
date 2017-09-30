using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
namespace Pulsar4X.CrossPlatformUI.Views.RefinaryView
{

    public class RefinaryView : TableLayout
    {
        protected StackLayout RefinaryJobs;
        protected ComboBox ItemComboBox;
        protected Button NewJobAdd;
        public RefinaryView()
        {
            XamlReader.Load(this);
            ItemComboBox.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string> m) => m.DisplayList);
            ItemComboBox.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string> m) => m.SelectedIndex);
            DataContextChanged += RefinaryView_DataContextChanged;
        }

        private void RefinaryView_DataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is RefiningVM)
            {
                RefiningVM dc = (RefiningVM)DataContext;
                dc.CurrentJobs.CollectionChanged += OnJobItemsChanged;
                OnJobItemsChanged(null, null);
            }
        }

        private void OnJobItemsChanged(object sender, EventArgs e)
        {
            RefiningVM dc = (RefiningVM)DataContext;
            RefinaryJobs.SuspendLayout();
            RefinaryJobs.Items.Clear();
            foreach (var item in dc.CurrentJobs)
            {
                var newJobUC = new JobUC();
                newJobUC.DataContext = item;
                RefinaryJobs.Items.Add(newJobUC);
            }
            RefinaryJobs.ResumeLayout();
        }
    }
}
