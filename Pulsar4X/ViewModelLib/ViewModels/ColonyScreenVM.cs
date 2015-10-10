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
        private Dictionary<Guid, RefinedMaterialSD> _materialsDictionary;
        private ObservableCollection<FacilityVM> _facilities;
        public ObservableCollection<FacilityVM> Facilities
        {
            get { return _facilities; }
        }

        private Dictionary<string, long> _species;
        public Dictionary<string, long> Species { get { return _species; } }

        public RawMineralStockpileVM RawMineralStockpile { get; set; }

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
                Facilities.Add(new FacilityVM(installation, ColonyInfo));
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

            RawMineralStockpile = new RawMineralStockpileVM(staticData, colonyEntity);

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



        private void SetupMatsStockpile()
        {
            var mats = _colonyEntity.GetDataBlob<ColonyInfoDB>().RefinedStockpile;
            _materialStockpile = new ObservableCollection<MatsStockpileInfoVM>();
            foreach (var kvp in mats)
            {
                RefinedMaterialSD mat = _materialsDictionary[kvp.Key];
                _materialStockpile.Add(new MatsStockpileInfoVM(kvp.Key, mat.Name, ColonyInfo));
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
            SetupMineralStockpile();
        }

        private void SetupMineralStockpile()
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
                SetupMineralStockpile();
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

    public class FacilityVM : IViewModel
    {
        private Entity _facilityEntity;
        private ColonyInfoDB _colonyInfo;
        private int _count;

        public string Name { get { return _facilityEntity.GetDataBlob<NameDB>().DefaultName; } }
        public int Count
        {
            get{ return _count; }
            private set {_count = value; OnPropertyChanged();}
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
            
            int count = 0;
            foreach (var installation in _colonyInfo.Installations)
            {
                if (installation.GetDataBlob<ComponentInfoDB>().DesignGuid 
                    == _facilityEntity.GetDataBlob<ComponentInfoDB>().DesignGuid)
                    count ++;
            }
            Count = count;
        }
    }


    public class RefinaryAbilityVM : IViewModel
    {
        private ColonyRefiningDB _refiningDB;
        private StaticDataStore _staticData;
        private ObservableCollection<RefinaryJobVM> _refinaryJobs;
        public ObservableCollection<RefinaryJobVM> RefinaryJobs
        {
            get { return _refinaryJobs;}
            set
            {
                _refinaryJobs = value;
                OnPropertyChanged();
            }
        }
        public RefinaryAbilityVM(StaticDataStore staticData, ColonyRefiningDB colonyRefining)
        {
            _staticData = staticData;
            _refiningDB = colonyRefining;
            SetupRefiningJobs();
        }



        private void SetupRefiningJobs()
        {
            var jobs = _refiningDB.JobBatchList;
            _refinaryJobs = new ObservableCollection<RefinaryJobVM>();
            foreach (var item in jobs)
            {
               _refinaryJobs.Add(new RefinaryJobVM(_staticData,_refiningDB,item));
            }
            RefinaryJobs = RefinaryJobs;
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
            if (_refiningDB.JobBatchList.Count != RefinaryJobs.Count)
                SetupRefiningJobs();
            else
            foreach (var job in RefinaryJobs)
            {
                job.Refresh();
            }
        }
    }

    public class RefinaryJobVM : IViewModel
    {
        private StaticDataStore _staticData;
        private RefineingJob _job;
        private ColonyRefiningDB _refiningDB;

        public string Material { get { return _staticData.RefinedMaterials[_job.jobGuid].Name; } }
        public int Remaining { get { return _refiningDB.RemainingJobs; } }
        public int BatchQuantity { get { return _job.numberOrdered; } }       
        public bool Repeat {get{return _job.auto;}}

        public RefinaryJobVM(StaticDataStore staticData, ColonyRefiningDB colonyRefiningDB, RefineingJob refiningJob)        
        {
            _staticData = staticData;
            _refiningDB = colonyRefiningDB;
            _job = refiningJob;
        }



        public event PropertyChangedEventHandler PropertyChanged;
        public void Refresh(bool partialRefresh = false)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("Remaining"));
                PropertyChanged(this, new PropertyChangedEventArgs("BatchQuantity"));
                PropertyChanged(this, new PropertyChangedEventArgs("Repeat"));
            }
        }
    }



}
