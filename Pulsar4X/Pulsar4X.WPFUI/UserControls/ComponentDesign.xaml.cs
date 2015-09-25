using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        private TechDB _factionTech;
        private StaticDataStore _staticData;

        public ComponentDesign()
        {                    
            InitializeComponent();
            componentDesignTemplates = new List<ComponentDesignDB>();
            foreach (var componentSD in App.Current.Game.StaticData.Components.Values)
            {

                _factionTech = App.Current.GameVM.PlayerFaction.GetDataBlob<TechDB>();
                _staticData = App.Current.Game.StaticData;
                ComponentDesignDB design = GenericComponentFactory.StaticToDesign(componentSD, _factionTech, _staticData);
                componentDesignTemplates.Add(design);                
            }
            ComponentSelection.ItemsSource = componentDesignTemplates;
            ComponentSelection.DisplayMemberPath = "Name";
            
        }

        private void ComponentSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ComponentDesignAbilitiesGrid.ItemsSource = componentDesignTemplates[ComponentSelection.SelectedIndex].ComponentDesignAbilities;

            foreach (var componentAbility in componentDesignTemplates[ComponentSelection.SelectedIndex].ComponentDesignAbilities)
            {
                ComponentAbilityDesignVM vm = new ComponentAbilityDesignVM(componentAbility, _factionTech, _staticData);
                if(vm.GuiControl != null)
                    AbilityStackPanel.Children.Add(vm.GuiControl);
            }


        }
    }
}
