using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.CrossPlatformUI.CustomControls;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class TimeControlView : Panel
    {
        public PausePlayButton btnPausePlay { get; set; }

        public TimeControlView()
        {
            XamlReader.Load(this);
        }
    }
}
