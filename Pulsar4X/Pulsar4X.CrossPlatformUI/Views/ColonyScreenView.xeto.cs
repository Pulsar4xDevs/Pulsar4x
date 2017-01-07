using Eto.Forms;
using Eto.Serialization.Xaml;
using Pulsar4X.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.CrossPlatformUI.Views
{
    public class ColonyScreenView : Panel
    {

        protected ComboBox ColonySelection { get; set; }
        //protected GridView FacDataGrid { get; set; }
        protected GenericStackControl FacilitysView;
        protected GridView PopDataGrid { get; set; }
        protected GridView MineralDeposits { get; set; }
        protected CargoView.CargoStorageView CargoView { get; set; }
        protected JobAbilityView RefineryAbilityView { get; set; }
        protected JobAbilityView ConstructionAbilityView { get; set; }
        protected ResearchAbilityView ResearchAbilityView { get; set; }


        private ColonyScreenVM _colonyScreenVM;
        private GameVM gameVM { get; set; }

        protected ColonyScreenView()
        {
            XamlReader.Load(this);
            FacilitysView.ControlType = typeof(ComponentListView.ComponentSpecificDesignView);
            ColonySelection.BindDataContext(c => c.DataStore, (DictionaryVM<Guid, string> m) => m.DisplayList);
            ColonySelection.SelectedIndexBinding.BindDataContext((DictionaryVM<Guid, string> m) => m.SelectedIndex);

            //FacDataGrid.Columns.Add(new GridColumn
            //{
            //    DataCell = new TextBoxCell { Binding = Binding.Property<FacilityVM, string>(r => r.Name) }
            //});

            PopDataGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property((KeyValuePair<string, long> r) => r.Key) },
                HeaderText = "Race"
            });
            PopDataGrid.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property((KeyValuePair<string, long> r) => r.Value).Convert(r => r.ToString("N0")) },
                HeaderText = "Population"
            });

            MineralDeposits.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<PlanetMineralInfoVM, string>(r => r.Mineral) },
                HeaderText = "Resource"
            });
            MineralDeposits.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<PlanetMineralInfoVM, double>(r => r.Accessability).Convert(r => r.ToString("F3")) },
                HeaderText = "Access."
            });
            MineralDeposits.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<PlanetMineralInfoVM, int>(r => r.Amount).Convert(r => r.ToString()) },
                HeaderText = "Stock"
            });
        }

        public ColonyScreenView(GameVM gameVM) :this()
        {
            this.gameVM = gameVM;

            ColonySelection.DataContext = gameVM.Colonys;
            //ColonySelection.SelectedKeyChanged += SetViewModel;
            gameVM.Colonys.SelectionChangedEvent += SetViewModel;
            SetViewModel(0, 0);
        }


        private void SetViewModel(int oldSelection, int newSelection)
        {

            _colonyScreenVM = gameVM.SelectedColonyScreenVM;
            DataContext = gameVM.SelectedColonyScreenVM;

            //FacDataGrid.DataStore = _colonyScreenVM.Facilities;

            PopDataGrid.DataStore = _colonyScreenVM.Species.Cast<object>();
            _colonyScreenVM.Species.CollectionChanged += Species_CollectionChanged;
            
            MineralDeposits.DataStore = _colonyScreenVM.PlanetMineralDepositVM.MineralDeposits.Values;
            gameVM.SelectedColonyScreenVM.PlanetMineralDepositVM.PropertyChanged += PlanetMineralDepositVM_PropertyChanged;

            CargoStorageVM cargoVM = new CargoStorageVM(gameVM);
            cargoVM.Initialise(_colonyScreenVM._colonyEntity);
            CargoView.DataContext = cargoVM;
            
            //gameVM.SelectedColonyScreenVM.RawMineralStockpileVM.PropertyChanged += RawMineralStockpileVM_PropertyChanged;
            //RefineryAbilityView = new JobAbilityView(colonyScreenVM.RefineryAbilityVM);
            RefineryAbilityView.SetViewModel(_colonyScreenVM.RefineryAbilityVM);
            

            ConstructionAbilityView.SetViewModel(_colonyScreenVM.ConstructionAbilityVM);
            //ResearchAbilityView = new ResearchAbilityView(colonyScreenVM.ColonyResearchVM);
            ResearchAbilityView.SetViewModel(_colonyScreenVM.ColonyResearchVM);

        }

        private void Species_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PopDataGrid.DataStore = _colonyScreenVM.Species.Cast<object>();
        }

        private void PlanetMineralDepositVM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            MineralDeposits.DataStore = _colonyScreenVM.PlanetMineralDepositVM.MineralDeposits.Values;
            
        }
        
    }
}
