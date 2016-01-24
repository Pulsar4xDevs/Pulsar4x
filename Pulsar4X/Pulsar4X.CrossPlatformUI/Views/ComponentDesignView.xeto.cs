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
        protected DynamicLayout AbilitysLayout {get;set;}
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

            ComponentSelection.ItemTextBinding = Binding.Property((KeyValuePair<Guid, string> r) => r.Value);
            ComponentSelection.ItemKeyBinding = Binding.Property((KeyValuePair<Guid, string> r) => r.Key).Convert(r => r.ToString());

            ComponentSelection.DataStore = _designVM.ComponentTypes.Cast<object>();
            ComponentSelection.SelectedKeyChanged += SetViewModel;

        }

        private void SetViewModel(object sender, EventArgs e)
        {
            _designVM.SetComponent((Guid)ComponentSelection.SelectedValue);
            foreach (var componentAbilityVM in _designVM.AbilityList)
            {
                switch (componentAbilityVM.GuiHint)
                {
                    case GuiHint.GuiTechSelectionList:
                        AbilitySelectionList asl = new AbilitySelectionList();
                        asl.GuiListSetup(componentAbilityVM);
                        //asl.ValueChanged += OnValueChanged;
                        AbilitysLayout.Add(asl);
                        break;
                    case GuiHint.GuiSelectionMaxMin:
                        MinMaxSlider mms = new MinMaxSlider();
                        //mms.GuiSliderSetup(componentAbilityVM);
                        //mms.ValueChanged += OnValueChanged;
                        AbilitysLayout.Add(mms);
                        break;
                }

                //componentAbilityVM.ValueChanged += OnValueChanged;
                //OnValueChanged(GuiHint.None, 0);
            }
        }
    }
}
