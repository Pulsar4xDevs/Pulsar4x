using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Pulsar4X.ECSLib;
using Pulsar4X.WPFUI.ViewModels;

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

                if (componentAbilityVM.GuiControl != null)
                    AbilityStackPanel.Children.Add(componentAbilityVM.GuiControl);
                componentAbilityVM.ValueChanged += OnValueChanged;
            }
        }

        private void OnValueChanged(object sender, double value)
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
