using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class PlayerOptionsDialog : Dialog
    {
        protected ComboBox PlayersComBox { get; set; }

        public PlayerOptionsDialog()
        {
            XamlReader.Load(this);
            PlayersComBox.BindDataContext(c => c.DataStore, (DictionaryVM<object, string> m) => m.DisplayList);
            PlayersComBox.SelectedIndexBinding.BindDataContext((DictionaryVM<object, string> m) => m.SelectedIndex);
        }

        protected void DefaultButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected void AbortButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected void GMButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
