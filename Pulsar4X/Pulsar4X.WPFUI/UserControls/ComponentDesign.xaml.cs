using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Pulsar4X.ECSLib;
using Pulsar4X.WPFUI.UserControls;
using Pulsar4X.ViewModels;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for ComponentDesign.xaml
    /// </summary>
    public partial class ComponentDesign : ITabControl
    {
        public string Title { get; set; }

        private List<ComponentDesignDB> componentDesignTemplates;
        private FactionTechDB _factionTech;
        private StaticDataStore _staticData;

        private ComponentDesignVM _selectedTemplate;

        public ComponentDesign()
        {                    
            InitializeComponent();
            Title = "Component Design";
            componentDesignTemplates = new List<ComponentDesignDB>();
            foreach (var componentSD in App.Current.Game.StaticData.Components.Values)
            {

                _factionTech = App.Current.GameVM.PlayerFaction.GetDataBlob<FactionTechDB>();
                _staticData = App.Current.Game.StaticData;
                ComponentDesignDB design = GenericComponentFactory.StaticToDesign(componentSD, _factionTech, _staticData);
                componentDesignTemplates.Add(design);                
            }
            ComponentSelection.ItemsSource = componentDesignTemplates;
            ComponentSelection.DisplayMemberPath = "Name";
            
        }

        private void ComponentSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedTemplate = new ComponentDesignVM(componentDesignTemplates[ComponentSelection.SelectedIndex], _staticData);

            foreach (var componentAbilityVM in _selectedTemplate.AbilityList)
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
            
            ComponentStats.Text = _selectedTemplate.StatsText;
            AbilityStats.Text = _selectedTemplate.AbilityStatsText;
            
        }

        private void ButtonCreate_Click(object sender, RoutedEventArgs e)
        {
            _selectedTemplate.DesignDB.Name = NameTextbox.Text;
            GenericComponentFactory.DesignToEntity(App.Current.Game.GlobalManager, _selectedTemplate.DesignDB, _factionTech);
        }


    }
}
