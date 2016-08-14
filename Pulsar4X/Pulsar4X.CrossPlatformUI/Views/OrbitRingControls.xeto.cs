using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class OrbitRingControls : Panel
    {
        public OrbitRingControls()
        {
            XamlReader.Load(this);
        }
    }
}
