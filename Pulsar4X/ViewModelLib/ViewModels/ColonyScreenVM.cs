using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using Pulsar4X.ECSLib;


namespace Pulsar4X.ViewModel
{
    public class ColonyScreenVM : IViewModel
    {
        private Entity _colonyEntity;
        private ColonyInfoDB ColonyInfo { get { return _colonyEntity.GetDataBlob<ColonyInfoDB>(); } }
        private Entity FactionEntity { get { return ColonyInfo.FactionEntity; } }
        private Dictionary<Guid, MineralSD> _mineralDictionary;

        private ObservableCollection<FacilityVM> _facilities;
        public ObservableCollection<FacilityVM> Facilities
        {
            get { return _facilities; }
        }

        private Dictionary<string, long> _species;
        public Dictionary<string, long> Species { get { return _species; } }

        public PlanetMineralDepositVM PlanetMineralDepositVM { get; set; }
        public RawMineralStockpileVM RawMineralStockpileVM { get; set; }
        public RefinedMatsStockpileVM RefinedMatsStockpileVM { get; set; }

        public RefinaryAbilityVM RefinaryAbilityVM { get; set; }
        public ConstructionAbilityVM ConstructionAbilityVM { get; set; }

        public ColonyResearchVM ColonyResearchVM { get; set; }





        public string ColonyName
        {
            get { return _colonyEntity.GetDataBlob<NameDB>().GetName(FactionEntity); }
            set
            {
                _colonyEntity.GetDataBlob<NameDB>().SetName(FactionEntity, value);
                OnPropertyChanged();
            }
        }

        public ColonyScreenVM()
        {
        }

        public ColonyScreenVM(Entity colonyEntity, StaticDataStore staticData)
        {


            _colonyEntity = colonyEntity;
            _facilities = new ObservableCollection<FacilityVM>();
            foreach (var installation in colonyEntity.GetDataBlob<ColonyInfoDB>().Installations)
            {
                Facilities.Add(new FacilityVM(installation.Key, ColonyInfo));
            }
            _species = new Dictionary<string, long>();

            foreach (var kvp in ColonyInfo.Population)
            {
                string name = kvp.Key.GetDataBlob<NameDB>().DefaultName;

                _species.Add(name, kvp.Value);
            }

            _mineralDictionary = new Dictionary<Guid, MineralSD>();
            foreach (var mineral in staticData.Minerals)
            {
                _mineralDictionary.Add(mineral.ID, mineral);
            }


            PlanetMineralDepositVM = new PlanetMineralDepositVM(staticData, _colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity);

            RawMineralStockpileVM = new RawMineralStockpileVM(staticData, _colonyEntity);

            RefinedMatsStockpileVM = new RefinedMatsStockpileVM(staticData, _colonyEntity);
            
            RefinaryAbilityVM = new RefinaryAbilityVM(staticData, _colonyEntity);

            ConstructionAbilityVM = new ConstructionAbilityVM(staticData, _colonyEntity);

            ColonyResearchVM = new ColonyResearchVM(staticData, _colonyEntity);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void Refresh(bool partialRefresh = false)
        {
            PlanetMineralDepositVM.Refresh();
            RawMineralStockpileVM.Refresh();
            RefinedMatsStockpileVM.Refresh();
            RefinaryAbilityVM.Refresh();
            ConstructionAbilityVM.Refresh();
            foreach (var facilityvm in Facilities)
            {
                facilityvm.Refresh();
            }
        }
    }


    public class PlanetMineralDepositVM : IViewModel
    {
        private Entity _planetEntity;
        private SystemBodyDB systemBodyInfo { get { return _planetEntity.GetDataBlob<SystemBodyDB>(); } }
        private Dictionary<Guid, MineralSD> _mineralDictionary;

        private ObservableCollection<PlanetMineralInfoVM> _mineralDeposits;
        public ObservableCollection<PlanetMineralInfoVM> MineralDeposits
        {
            get { return _mineralDeposits; }
            set { _mineralDeposits = value; OnPropertyChanged(); }
        }


        public PlanetMineralDepositVM(StaticDataStore staticData, Entity planetEntity)
        {
            _mineralDictionary = new Dictionary<Guid, MineralSD>();
            foreach (var mineral in staticData.Minerals)
            {
                _mineralDictionary.Add(mineral.ID, mineral);
            }
            _planetEntity = planetEntity;
            Initialise();
        }

        private void Initialise()
        {
            var minerals = systemBodyInfo.Minerals;
            _mineralDeposits = new ObservableCollection<PlanetMineralInfoVM>();
            foreach (var kvp in minerals)
            {
                MineralSD mineral = _mineralDictionary[kvp.Key];

                _mineralDeposits.Add(new PlanetMineralInfoVM(mineral.Name, kvp.Value));
            }
            MineralDeposits = MineralDeposits;
        }




        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void Refresh(bool partialRefresh = false)
        {
            if (systemBodyInfo.Minerals.Count != MineralDeposits.Count)
                Initialise();
            else
            foreach (var mineralvm in MineralDeposits)
            {
                mineralvm.Refresh();
            }
        }
    }

    public class PlanetMineralInfoVM : IViewModel
    {
        private MineralDepositInfo _mineralDepositInfo;

        public string Mineral { get; private set; }
        public int Amount { get { return _mineralDepositInfo.Amount; } }
        public double Accessability { get { return _mineralDepositInfo.Accessibility; } }

        public PlanetMineralInfoVM(string name, MineralDepositInfo deposit)
        {
            Mineral = name;
            _mineralDepositInfo = deposit;           
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Refresh(bool partialRefresh = false)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Amount"));
                PropertyChanged(this, new PropertyChangedEventArgs("Accessability"));
            }
        }
    }


    public class RawMineralStockpileVM : IViewModel
    {
        private Entity _colonyEntity;
        private ColonyInfoDB ColonyInfo { get { return _colonyEntity.GetDataBlob<ColonyInfoDB>(); } }
        private Dictionary<Guid, MineralSD> _mineralDictionary;

        private ObservableCollection<RawMineralInfoVM> _mineralStockpile;
        public ObservableCollection<RawMineralInfoVM> MineralStockpile
        {
            get { return _mineralStockpile; }
            set { _mineralStockpile = value; OnPropertyChanged(); }
        }

        public RawMineralStockpileVM(StaticDataStore staticData, Entity colonyEntity)
        {
            _mineralDictionary = new Dictionary<Guid, MineralSD>();
            foreach (var mineral in staticData.Minerals)
            {
                _mineralDictionary.Add(mineral.ID, mineral);
            }
            _colonyEntity = colonyEntity;
            Initialise();
        }

        private void Initialise()
        {
            var rawMinerals = ColonyInfo.MineralStockpile;
            _mineralStockpile = new ObservableCollection<RawMineralInfoVM>();
            foreach (var kvp in rawMinerals)
            {
                MineralSD mineral = _mineralDictionary[kvp.Key];
                _mineralStockpile.Add(new RawMineralInfoVM(kvp.Key, mineral.Name, ColonyInfo));
            }
            MineralStockpile = _mineralStockpile;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void Refresh(bool partialRefresh = false)
        {
            if (ColonyInfo.MineralStockpile.Count != MineralStockpile.Count)
                Initialise();
            else
                foreach (var mineral in MineralStockpile)
                {
                    mineral.Refresh();
                }
        }
    }
    public class RawMineralInfoVM : IViewModel
    {
        private ColonyInfoDB _colonyInfo;
        private Guid _guid;
        public string Mineral { get; private set; }
        public int Amount { get { return _colonyInfo.MineralStockpile[_guid]; } }

        public RawMineralInfoVM(Guid guid, string name, ColonyInfoDB colonyInfo)
        {
            _guid = guid;
            Mineral = name;
            _colonyInfo = colonyInfo;           
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Refresh(bool partialRefresh = false)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Amount"));
            }
        }
    }

    public class RefinedMatsStockpileVM : IViewModel
    {

        private Entity _colonyEntity;
        private ColonyInfoDB ColonyInfo { get { return _colonyEntity.GetDataBlob<ColonyInfoDB>(); } }
        private Dictionary<Guid, RefinedMaterialSD> _materialsDictionary;


        private ObservableCollection<RefinedMatInfoVM> _materialStockpile;
        public ObservableCollection<RefinedMatInfoVM> MaterialStockpile
        {
            get { return _materialStockpile; }
            set { _materialStockpile = value; OnPropertyChanged(); }
        }


        public RefinedMatsStockpileVM(StaticDataStore staticData, Entity colonyEntity)
        {
            _materialsDictionary = staticData.RefinedMaterials;
            _colonyEntity = colonyEntity;
            Initialise();
        }

        private void Initialise()
        {
            var mats = _colonyEntity.GetDataBlob<ColonyInfoDB>().RefinedStockpile;
            _materialStockpile = new ObservableCollection<RefinedMatInfoVM>();
            foreach (var kvp in mats)
            {
                RefinedMaterialSD mat = _materialsDictionary[kvp.Key];
                _materialStockpile.Add(new RefinedMatInfoVM(kvp.Key, mat.Name, ColonyInfo));
            }
            MaterialStockpile = MaterialStockpile;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void Refresh(bool partialRefresh = false)
        {
            if (ColonyInfo.MineralStockpile.Count != MaterialStockpile.Count)
                Initialise();
            else
                foreach (var item in MaterialStockpile)
                {
                    item.Refresh();
                }
        }
    }
    public class RefinedMatInfoVM : IViewModel
    {
        private ColonyInfoDB _colonyInfo;
        private Guid _guid;
        public string Material { get; private set; }
        public int Amount { get { return _colonyInfo.RefinedStockpile[_guid]; } }

        public RefinedMatInfoVM(Guid guid, string name, ColonyInfoDB colonyInfo)
        {
            _guid = guid;
            Material = name;
            _colonyInfo = colonyInfo;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Refresh(bool partialRefresh = false)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Amount"));
            }
        }
    }


    public class FacilityVM : IViewModel
    {
        private Entity _facilityEntity;
        private ColonyInfoDB _colonyInfo;

        public string Name { get { return _facilityEntity.GetDataBlob<NameDB>().DefaultName; } }
        public int Count
        {
            get{ return _colonyInfo.Installations[_facilityEntity];}
        }
        public int WorkersRequired { get { return _facilityEntity.GetDataBlob<ComponentInfoDB>().CrewRequrements * Count; } }

        public FacilityVM()
        {
        }

        public FacilityVM(Entity facilityEntity, ColonyInfoDB colonyInfo)
        {
            _facilityEntity = facilityEntity;
            _colonyInfo = colonyInfo;
            Refresh();

        }
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void Refresh(bool partialRefresh = false)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("Count"));
        }
    }


}
