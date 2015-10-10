using System;
using System.Windows;
using System.Windows.Controls;
using Pulsar4X.ECSLib;
using Pulsar4X.ViewModel;
using Pulsar4X.WPFUI.UserControls;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for ComponentDesign.xaml
    /// </summary>
    public partial class ComponentDesign : ITabControl
    {
        public string Title { get; set; }



        private ComponentDesignVM _designVM;

        public ComponentDesign()
        {                    
            InitializeComponent();
            Title = "Component Design";
            _designVM = ComponentDesignVM.Create(App.Current.GameVM);

            ComponentSelection.ItemsSource = _designVM.ComponentTypes; //componentDesignTemplates;
            ComponentSelection.DisplayMemberPath = "Key";
            ComponentSelection.SelectedValuePath = "Value";

        }

        private void ComponentSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _designVM.SetComponent((Guid)ComponentSelection.SelectedValue);
            foreach (var componentAbilityVM in _designVM.AbilityList)
            {
                switch (componentAbilityVM.GuiHint)
                {
                    case GuiHint.GuiTechSelectionList:
                        AbilitySelectionList asl = new AbilitySelectionList();
                        asl.GuiListSetup(componentAbilityVM);
                        asl.ValueChanged += OnValueChanged;
                        AbilityStackPanel.Children.Add(asl);
                        break;
                    case GuiHint.GuiSelectionMaxMin:
                        MinMaxSlider mms = new MinMaxSlider();
                        mms.GuiSliderSetup(componentAbilityVM);
                        mms.ValueChanged += OnValueChanged;
                        AbilityStackPanel.Children.Add(mms);
                        break;
                }
                
                componentAbilityVM.ValueChanged += OnValueChanged;
                OnValueChanged(GuiHint.None, 0);
            }

        }

        private void OnValueChanged(GuiHint controlType, double value)
        {
            
            ComponentStats.Text = _designVM.StatsText;
            AbilityStats.Text = _designVM.AbilityStatsText;
            
        }

        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            _designVM.DesignDB.Name = NameTextbox.Text;
            _designVM.CreateComponent();
        }


    }
}
