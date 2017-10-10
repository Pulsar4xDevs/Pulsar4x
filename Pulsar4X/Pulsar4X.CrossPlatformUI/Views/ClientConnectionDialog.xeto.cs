using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ClientConnectionDialog : Dialog
    {
        public ClientConnectionDialog()
        {
            XamlReader.Load(this);
            this.DataContextChanged += Handle_DataContextChanged;
        }

        void Handle_DataContextChanged(object sender, EventArgs e)
        {
            if (DataContext is ClientConnectionVM vm)
            { }
        }
    }
}