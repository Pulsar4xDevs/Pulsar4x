using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;


namespace Pulsar4X.ViewModels
{
    public class ColonyScreenVM : IViewModel
    {
        private Entity _colonyEntity;
        private ColonyInfoDB ColonyInfo { get { return _colonyEntity.GetDataBlob<ColonyInfoDB>(); } }
        private Entity FactionEntity { get { return ColonyInfo.FactionEntity; } }
        private Dictionary<Guid, MineralSD> _mineralDictionary;
        private Dictionary<Guid, RefinedMaterialSD> _materialsDictionary;
        private ObservableCollection<FacilityVM> _facilities;
        public ObservableCollection<FacilityVM> Facilities
        {
            get { return _facilities; }
        }

        private Dictionary<string, long> _species;
        public Dictionary<string, long> Species { get { return _species; } }

        private ObservableCollection<MineralInfoVM> _mineralDeposits;
        public ObservableCollection<MineralInfoVM> MineralDeposits
        {
            get { return _mineralDeposits; }
            private set
            {
                _mineralDeposits = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<RawStockpileInfoVM> _mineralStockpile;
        public ObservableCollection<RawStockpileInfoVM> MineralStockpile
        {
            get { return _mineralStockpile; }
            set { _mineralStockpile = value; OnPropertyChanged(); }
        }

        private ObservableCollection<MatsStockpileInfoVM> _materialStockpile;
        public ObservableCollection<MatsStockpileInfoVM> MaterialStockpile
        {
            get { return _materialStockpile; }
            set { _materialStockpile = value; OnPropertyChanged(); }
        }

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
                Facilities.Add(new FacilityVM(installation, FactionEntity));
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
            _materialsDictionary = staticData.RefinedMaterials;


            SetupMineralDeposoits();

            SetupMineralStockpile();

            SetupMatsStockpile();

        }


        private void SetupMineralDeposoits()
        {
            Entity planet = ColonyInfo.PlanetEntity;
            var minerals = planet.GetDataBlob<SystemBodyDB>().Minerals;
            _mineralDeposits = new ObservableCollection<MineralInfoVM>();
            foreach (var kvp in minerals)
            {
                MineralSD mineral = _mineralDictionary[kvp.Key];

                _mineralDeposits.Add(new MineralInfoVM(mineral.Name, kvp.Value));
            }
            MineralDeposits = MineralDeposits;
        }

        private void SetupMineralStockpile()
        {
            var rawMinerals = ColonyInfo.MineralStockpile;
            _mineralStockpile = new ObservableCollection<RawStockpileInfoVM>();
            foreach (var kvp in rawMinerals)
            {
                MineralSD mineral = _mineralDictionary[kvp.Key];
                _mineralStockpile.Add(new RawStockpileInfoVM(kvp.Key, mineral.Name, ColonyInfo));
            }
            MineralStockpile = MineralStockpile;
        }

        private void SetupMatsStockpile()
        {
            var mats = _colonyEntity.GetDataBlob<ColonyInfoDB>().RefinedStockpile;
            _materialStockpile = new ObservableCollection<MatsStockpileInfoVM>();
            foreach (var kvp in mats)
            {
                RefinedMaterialSD mat = _materialsDictionary[kvp.Key];
                _mineralStockpile.Add(new RawStockpileInfoVM(kvp.Key, mat.Name, ColonyInfo));
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

            
            foreach (var mineralvm in MineralDeposits)
            {
                mineralvm.Refresh();
            }

            if (ColonyInfo.MineralStockpile.Count != MineralStockpile.Count)
                SetupMineralStockpile();
            else
                foreach (var mineral in MineralStockpile)
                {
                    mineral.Refresh();
                }

            if (ColonyInfo.RefinedStockpile.Count != MaterialStockpile.Count)
                SetupMatsStockpile();
            else
            foreach (var mat in MaterialStockpile)
            {
                mat.Refresh();
            }
        }
    }

    public class MineralInfoVM : IViewModel
    {
        private MineralDepositInfo _mineralDepositInfo;

        public string Mineral { get; private set; }
        public int Amount { get { return _mineralDepositInfo.Amount; } }
        public double Accessability { get { return _mineralDepositInfo.Accessibility; } }

        public MineralInfoVM(string name, MineralDepositInfo deposit)
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

    public class RawStockpileInfoVM : IViewModel
    {
        private ColonyInfoDB _colonyInfo;
        private Guid _guid;
        public string Mineral { get; private set; }
        public int Amount { get { return _colonyInfo.MineralStockpile[_guid]; } }

        public RawStockpileInfoVM(Guid guid, string name, ColonyInfoDB colonyInfo)
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

    public class MatsStockpileInfoVM : IViewModel
    {
        private ColonyInfoDB _colonyInfo;
        private Guid _guid;
        public string Material { get; private set; }
        public int Amount { get { return _colonyInfo.RefinedStockpile[_guid]; } }

        public MatsStockpileInfoVM(Guid guid, string name, ColonyInfoDB colonyInfo)
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

    public class FacilityVM
    {
        private Entity _facilityEntity;
        private Entity _factionEntity;

        public string Name { get { return _facilityEntity.GetDataBlob<NameDB>().DefaultName; } }

        public int WorkersRequired { get { return _facilityEntity.GetDataBlob<ComponentInfoDB>().CrewRequrements; } }

        public FacilityVM()
        {
        }

        public FacilityVM(Entity facilityEntity, Entity factionEntity)
        {
            _facilityEntity = facilityEntity;
            _factionEntity = factionEntity;
        }
    }
}
