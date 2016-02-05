using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Forms;
using Eto.Serialization.Xaml;
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


            //ColonySelection.ItemTextBinding = Binding.Property((KeyValuePair<Guid, string> r) => r.Value);
            //ColonySelection.ItemKeyBinding = Binding.Property((KeyValuePair<Guid, string> r) => r.Key).Convert(r => r.ToString());

            //ColonySelection.DataStore = gameVM.Colonys.Cast<object>();
            ColonySelection.DataStore = gameVM.Colonys.DisplayList;
            //ColonySelection.SelectedKeyBinding.Convert(r => new Guid(r), g => g.ToString()).BindDataContext((GameVM m) => m.SetColonyScreen);
            //Guid guid1 = ColonySelection.SelectedKeyBinding.Convert(new Guid(r),)
            ColonySelection.SelectedKeyChanged += SetViewModel;

        }

        private void SetViewModel(object sender, EventArgs e)
        {
            colonyScreenVM = gameVM.ColonyScreens[0];

            FacDataGrid.DataStore = colonyScreenVM.Facilities;
            FacDataGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<FacilityVM, string>(r => r.Name) }
            });

            PopDataGrid.DataStore = colonyScreenVM.Species.Cast<object>();
            PopDataGrid.Columns.Add(new GridColumn
                {
                DataCell = new TextBoxCell{Binding = Binding.Property((KeyValuePair<string,long> r) => r.Key)}});
            PopDataGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property((KeyValuePair<string, long> r) => r.Value).Convert(r=> r.ToString()) }
            });
            
            MineralDeposits.DataStore = colonyScreenVM.PlanetMineralDepositVM.MineralDeposits;
            MineralDeposits.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<PlanetMineralInfoVM, string>(r => r.Mineral) }
            });
            MineralDeposits.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<PlanetMineralInfoVM, double>(r => r.Accessability).Convert(r=> r.ToString()) }
            });
            MineralDeposits.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<PlanetMineralInfoVM, int>(r => r.Amount).Convert(r => r.ToString()) }
            });

            MineralStockpile.DataStore = colonyScreenVM.RawMineralStockpileVM.MineralStockpile;
            MineralStockpile.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<RawMineralInfoVM, string>(r => r.Mineral) }
            });
            MineralStockpile.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<RawMineralInfoVM, int>(r => r.Amount).Convert(r => r.ToString()) }
            });


            //RefinaryAbilityView = new JobAbilityView(colonyScreenVM.RefinaryAbilityVM);
            RefinaryAbilityView.SetViewModel(colonyScreenVM.RefinaryAbilityVM);
            
            RefinedMats.DataStore = colonyScreenVM.RefinedMatsStockpileVM.MaterialStockpile;
            RefinedMats.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<RefinedMatInfoVM, string>(r => r.Material) }
            });
            RefinedMats.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<RefinedMatInfoVM, int>(r => r.Amount).Convert(r => r.ToString()) }
            });

            ConstructionAbilityView.SetViewModel(colonyScreenVM.ConstructionAbilityVM); 
            ResearchAbilityView = new ResearchAbilityView(colonyScreenVM.ColonyResearchVM);



        }
    }
}
