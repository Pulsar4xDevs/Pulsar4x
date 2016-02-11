using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ComponentListComponentView : Panel
    {
        private ComponentListComponentVM _viewModel;

        public ComponentListComponentView()
        {
            XamlReader.Load(this);
        }

        public ComponentListComponentView(ComponentListComponentVM viewModel) : this()
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
        }
    }
}
