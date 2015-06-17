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

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for Races.xaml
    /// </summary>
    public partial class Races : ITabControl
    {
        public string Title { get; set; }
        private Entity _faction;
        private FactionAbilitiesDB _abilities;

        public Races()
        {
            Title = "Races";
            InitializeComponent();
            //quick hack to get the player faction
            //todo fix this, maybe generate the window with a faction to view
            //_faction = Game.Instance.GlobalManager.GetFirstEntityWithDataBlob<FactionDB>();
            // Bug: This causes a NullRef exception because I broke Game.Instance.GlobalManager
            _abilities = _faction.GetDataBlob<FactionAbilitiesDB>();
            FactionAbilities_GroupBox.DataContext = _abilities;
        }

        private void ConstructionModifierButton_Click(object sender, RoutedEventArgs e)
        {
            _abilities.AbilityBonuses[AbilityType.GenericConstruction] += 0.01f;
        }

        private void SensorModifierButton_Click(object sender, RoutedEventArgs e)
        {
            _abilities.BasePlanetarySensorStrength += 1;
        }
    }
}
