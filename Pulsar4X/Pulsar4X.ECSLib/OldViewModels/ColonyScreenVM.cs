using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

/*
namespace Pulsar4X.ECSLib
{
    public class ColonyScreenVM : IViewModel
    {
        public Entity _colonyEntity;
        private ColonyInfoDB ColonyInfo { get { return _colonyEntity.GetDataBlob<ColonyInfoDB>(); } }
        private Entity _factionEntity;
        public CargoStorageVM CargoStoreVM { get; set; }
        private ObservableCollection<FacilityVM> _facilities;
        public ObservableCollection<FacilityVM> Facilities
        {
            get { return _facilities; }
        }

        public ObservableCollection<ComponentSpecificDesignVM> FacilitesList { get; } = new ObservableCollection<ComponentSpecificDesignVM>();

        private readonly ObservableDictionary<string, long> _species = new ObservableDictionary<string, long>();
        public ObservableDictionary<string, long> Species { get { return _species; } }

        public PlanetMineralDepositVM PlanetMineralDepositVM { get; set; }

        public RefineryAbilityVM RefineryAbilityVM { get; set; }
        //public ConstructionAbilityVM ConstructionAbilityVM { get; set; }

        public ColonyResearchVM ColonyResearchVM { get; set; }


        public string ColonyName
        {
            get { return _colonyEntity.GetDataBlob<NameDB>().GetName(_factionEntity.Guid); }
            set
            {
                _colonyEntity.GetDataBlob<NameDB>().SetName(_factionEntity.Guid, value);
                OnPropertyChanged();
            }
        }


        public ColonyScreenVM(GameVM gameVM, Entity colonyEntity, StaticDataStore staticData)
        {

            gameVM.Game.GamePulse.GameGlobalDateChangedEvent += GameVM_DateChangedEvent;
            _colonyEntity = colonyEntity;
            _colonyEntity.Manager.FindEntityByGuid(_colonyEntity.FactionOwner, out _factionEntity);
            _facilities = new ObservableCollection<FacilityVM>();
            ComponentInstancesDB instaces = colonyEntity.GetDataBlob<ComponentInstancesDB>();
            //foreach (var installation in instaces.GetComponentsByDesign)
            //{
                //Facilities.Add(new FacilityVM(installation.Key, instaces));
                //FacilitesList.Add(new ComponentSpecificDesignVM(installation.Key, installation.Value));
            //}


            UpdatePop();

            CommandReferences cmdRef = new CommandReferences(_colonyEntity.FactionOwner, _colonyEntity.Guid, gameVM.Game.OrderHandler, _colonyEntity.Manager.ManagerSubpulses);
            CargoStoreVM = new CargoStorageVM(staticData, cmdRef, colonyEntity.GetDataBlob<CargoStorageDB>());

            PlanetMineralDepositVM = new PlanetMineralDepositVM(staticData, _colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity);
         
            RefineryAbilityVM = new RefineryAbilityVM(staticData, _colonyEntity);

            //ConstructionAbilityVM = new ConstructionAbilityVM(staticData, _colonyEntity);

            ColonyResearchVM = new ColonyResearchVM(staticData, _colonyEntity);
        }

        private void UpdatePop()
        {
            _species.Clear();
            foreach (var kvp in ColonyInfo.Population)
            {
                string name = kvp.Key.GetDataBlob<NameDB>().DefaultName;
                _species.Add(name, kvp.Value);
            }
        }

        private void GameVM_DateChangedEvent(DateTime newDate)
        {
            Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void Refresh(bool partialRefresh = false)
        {
            PlanetMineralDepositVM.Refresh();
            RefineryAbilityVM.Refresh();
            //ConstructionAbilityVM.Refresh();
            UpdatePop();
            CargoStoreVM.Update();
            foreach (var facilityvm in Facilities)
            {
                facilityvm.Refresh();
            }
        }
    }

    
    public class PlanetMineralDepositVM : IViewModel
    {
        private Entity _planetEntity;
        private SystemBodyInfoDB systemBodyInfo { get { return _planetEntity.GetDataBlob<SystemBodyInfoDB>(); } }
        private StaticDataStore _staticData;
        private readonly ObservableDictionary<Guid, PlanetMineralInfoVM> _mineralDeposits = new ObservableDictionary<Guid, PlanetMineralInfoVM>();
        public ObservableDictionary<Guid, PlanetMineralInfoVM> MineralDeposits { get { return _mineralDeposits; } }



        public PlanetMineralDepositVM(StaticDataStore staticData, Entity planetEntity)
        {
            _planetEntity = planetEntity;
            _staticData = staticData;
            Initialise();
        }

        private void Initialise()
        {
            var minerals = systemBodyInfo.Minerals;
            _mineralDeposits.Clear();
            foreach (var kvp in minerals)
            {
                MineralSD mineral = _staticData.CargoGoods.GetMineral(kvp.Key);
                if (!_mineralDeposits.ContainsKey(kvp.Key))
                    _mineralDeposits.Add(kvp.Key, new PlanetMineralInfoVM(mineral.Name, kvp.Value));
            }
            OnPropertyChanged(nameof(MineralDeposits));

        }




        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void Refresh(bool partialRefresh = false)
        {
            if (systemBodyInfo.Minerals.Count != MineralDeposits.Count)
                Initialise();
            else
                foreach (var mineralvm in MineralDeposits.Values)
                {
                    mineralvm.Refresh();
                }
            OnPropertyChanged();
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
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Amount)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Accessability)));

            }
        }
    }



    public class FacilityVM : IViewModel
    {
        private Entity _facilityEntity;
        //private ColonyInfoDB _colonyInfo;
        private ComponentInstancesDB _componentInstancesDB;

        public string Name { get { return _facilityEntity.GetDataBlob<NameDB>().DefaultName; } }
        public int Count
        {
            get { return _componentInstancesDB.GetNumberOfComponentsOfDesign(_facilityEntity.Guid); }//_colonyInfo.Installations[_facilityEntity];}
        }
        public int WorkersRequired { get { return _facilityEntity.GetDataBlob<ComponentInfoDB>().CrewRequrements * Count; } }

        public FacilityVM()
        {
        }

        public FacilityVM(Entity facilityEntity, ComponentInstancesDB componentInstances)
        {
            _facilityEntity = facilityEntity;
            //_colonyInfo = colonyInfo;
            _componentInstancesDB = componentInstances;
            Refresh();

        }
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void Refresh(bool partialRefresh = false)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
        }
    }
}
*/