using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel;
using System;

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
            ComponentSelection.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string> m) => m.DisplayList);
            ComponentSelection.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string> m) => m.SelectedIndex);
        }
        public ComponentDesignView(ComponentDesignVM viewmodel) :this()
        {
            _designVM = viewmodel;
            DataContext = viewmodel;

            viewmodel.ComponentTypes.SelectionChangedEvent += SetViewModel;
            Create.Click += Create_Click;
            SetViewModel(0, 0);
        }

        void Create_Click(object sender, EventArgs e)
        {
            _designVM.Design.Name = Name.Text;
            _designVM.CreateComponent();
        }

        private void SetViewModel(int oldindex, int newindex)
        {
            
            _designVM.SetComponent(_designVM.ComponentTypes.GetValue(newindex));  //(Guid)ComponentSelection.SelectedValue);
            AbilitysLayout.Items.Clear();
            AbilitysLayout.SuspendLayout();
            foreach (var componentAbilityVM in _designVM.AbilityList)
            {
                switch (componentAbilityVM.GuiHint)
                {
                    case GuiHint.GuiTechSelectionList:
                        AbilitySelectionList asl = new AbilitySelectionList(componentAbilityVM);
                        AbilitysLayout.Items.Add(asl);
                        break;
                    case GuiHint.GuiSelectionMaxMin:
                        MinMaxSlider mms = new MinMaxSlider(componentAbilityVM.minMaxSliderVM) {};
                        AbilitysLayout.Items.Add(mms);
                        break;
                }
                Name.Text = _designVM.Design.Name;

                ComponentStats.Text = _designVM.StatsText;
                AbilityStats.Text = _designVM.AbilityStatsText;
            }
            AbilitysLayout.ResumeLayout();
            
        }
    }
}
