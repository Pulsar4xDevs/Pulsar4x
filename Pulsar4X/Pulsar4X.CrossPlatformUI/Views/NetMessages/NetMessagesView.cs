using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
using System;

namespace Pulsar4X.CrossPlatformUI.Views.NetMessages
{
    public class NetMessagesView : Panel
    {

        public NetMessagesView()
        {
            XamlReader.Load(this);
            DataContextChanged += NetMessagesView_DataContextChanged; 
        }


        void NetMessagesView_DataContextChanged(object sender, EventArgs e)
        {

        }
    }
}
