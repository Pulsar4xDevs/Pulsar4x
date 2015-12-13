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
    public class ColonyScreenView : Panel
    {

        protected ComboBox ColonySelection { get; set; }
        protected GridView FacDataGrid { get; set; }
        protected GridView PopDataGrid { get; set; }
        protected GridView MineralDeposits { get; set; }
        protected GridView MineralStockpile { get; set; }
        protected JobAbilityView RefinaryAbilityView { get; set; }
        protected GridView RefinedMats { get; set; }
        protected JobAbilityView ConstructionAbilityView { get; set; }
        protected ResearchAbilityView ResearchAbilityView { get; set; }


        private ColonyScreenVM colonyScreenVM { get; set; }
        private GameVM gameVM { get; set; }

        protected ColonyScreenView()
        {
            XamlReader.Load(this);
        }

        public ColonyScreenView(GameVM gameVM) :this()
        {
            this.gameVM = gameVM;
            

            ColonySelection.ItemTextBinding = Binding.Property((KeyValuePair<Guid, string> r) => r.Value);
            ColonySelection.ItemKeyBinding = Binding.Property((KeyValuePair<Guid, string> r) => r.Key).Convert(r => r.ToString());
            
            ColonySelection.DataStore = gameVM.Colonys.Cast<object>();
            //ColonySelection.SelectedKeyBinding.Convert(r => new Guid(r), g => g.ToString()).BindDataContext((GameVM m) => m.SetColonyScreen);
            //Guid guid1 = ColonySelection.SelectedKeyBinding.Convert(new Guid(r),)
            ColonySelection.SelectedKeyChanged += SetViewModel;

        }

        private void SetViewModel(object sender, EventArgs e)
        {
            colonyScreenVM = gameVM.ColonyScreens[0];

            FacDataGrid.DataContext = colonyScreenVM.Facilities;
            PopDataGrid.DataContext = colonyScreenVM.Species;
            MineralDeposits.DataContext = colonyScreenVM.PlanetMineralDepositVM;
            MineralStockpile.DataContext = colonyScreenVM.RawMineralStockpileVM;
            RefinaryAbilityView = new JobAbilityView(colonyScreenVM.RefinaryAbilityVM);
            RefinedMats.DataContext = colonyScreenVM.RefinedMatsStockpileVM;
            ConstructionAbilityView = new JobAbilityView(colonyScreenVM.ConstructionAbilityVM);
            ResearchAbilityView = new ResearchAbilityView(colonyScreenVM.ColonyResearchVM);



        }
    }
}
