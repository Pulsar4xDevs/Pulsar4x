using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ComponentDesignView : Panel
    {
        protected ComboBox ComponentSelection {get;set;}
        protected DynamicLayout AbilitysLayout {get;set;}
        protected TextBox AbilityStats { get; set; }
        protected TextBox ComponentStats { get; set; }
        protected Button Create { get; set; }
        protected TextBox Name { get; set; }



        public ComponentDesignView()
        {
            XamlReader.Load(this);
        }
        public ComponentDesignView(ComponentDesignVM viewmodel) :this()
        {
           
        
        }
    }
}
