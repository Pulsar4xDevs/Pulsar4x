using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ResearchAbilityView : Panel
    {
        public ResearchAbilityView()
        {
            XamlReader.Load(this);
        }

        public ResearchAbilityView(ColonyResearchVM viewModel) :this()
        { 
        }

    }
}
