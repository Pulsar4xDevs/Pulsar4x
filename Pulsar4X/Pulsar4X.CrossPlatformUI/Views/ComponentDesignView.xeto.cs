using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ComponentDesignView : Panel
    {
        protected ComboBox ComponentSelection {get;set;}
        protected StackLayout AbilitysLayout {get;set;}
        protected TextBox AbilityStats { get; set; }
        protected TextBox ComponentStats { get; set; }
        protected Button Create { get; set; }
        protected TextBox Name { get; set; }

        private ComponentDesignVM _designVM;

        public ComponentDesignView()
        {
            XamlReader.Load(this);
        }
        public ComponentDesignView(ComponentDesignVM viewmodel) :this()
        {
            _designVM = viewmodel;
            DataContext = viewmodel;

            ComponentSelection.DataStore = _designVM.ComponentTypes.DisplayList;          
            ComponentSelection.SelectedKeyChanged += SetViewModel;
            Create.Click += Create_Click;

        }

        void Create_Click(object sender, EventArgs e)
        {
            _designVM.DesignDB.Name = Name.Text;
            _designVM.CreateComponent();
        }

        private void SetViewModel(object sender, EventArgs e)
        {
            _designVM.SetComponent(_designVM.ComponentTypes.GetValue(ComponentSelection.SelectedIndex));  //(Guid)ComponentSelection.SelectedValue);
            
            foreach (var componentAbilityVM in _designVM.AbilityList)
            {
                switch (componentAbilityVM.GuiHint)
                {
                    case GuiHint.GuiTechSelectionList:
                        AbilitySelectionList asl = new AbilitySelectionList(componentAbilityVM);
                        //asl.GuiListSetup(componentAbilityVM);
                        asl.ValueChanged += OnValueChanged;
                        AbilitysLayout.Items.Add(asl);
                        break;
                    case GuiHint.GuiSelectionMaxMin:
                        MinMaxSlider mms = new MinMaxSlider(componentAbilityVM.MinMaxSlider);
                        mms.ValueChanged += OnValueChanged;
                        AbilitysLayout.Items.Add(mms);
                        break;
                }
                Name.Text = _designVM.DesignDB.Name;
                componentAbilityVM.ValueChanged += OnValueChanged;
                OnValueChanged(GuiHint.None, 0);
            }
        }

        private void OnValueChanged(GuiHint controlType, double value)
        {

            ComponentStats.Text = _designVM.StatsText;
            AbilityStats.Text = _designVM.AbilityStatsText;

        }
    }
}
