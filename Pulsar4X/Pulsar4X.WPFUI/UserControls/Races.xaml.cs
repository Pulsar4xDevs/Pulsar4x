using Pulsar4X.ECSLib;
using System.Windows;

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
