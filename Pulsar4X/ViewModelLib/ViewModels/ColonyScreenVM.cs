using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;


namespace Pulsar4X.ViewModels
{
    public class ColonyScreenVM : IViewModel
    {
        private Entity _colonyEntity;
        private Entity _factionEntity;
        private Dictionary<Guid, MineralSD> _mineralDictionary; 
        private ObservableCollection<FacilityVM> _facilities;
        public ObservableCollection<FacilityVM> Facilities
        {
            get { return _facilities; }
        }

        private Dictionary<string, long> _species;
        public Dictionary<string, long> Species { get { return _species; } }

        private Dictionary<string, MineralDepositInfo> _mineralDeposits;
        public Dictionary<string, MineralDepositInfo> MineralDeposits { get { return _mineralDeposits; } }

        private Dictionary<string, int> _mineralStockpile;
        public Dictionary<string, int> MineralStockpile { get { return _mineralStockpile; } }

        private Dictionary<string, int> _materialStockpile;
        public Dictionary<string, int> MaterialStockpile { get { return _materialStockpile; } }

        public string ColonyName
        {
            get { return _colonyEntity.GetDataBlob<NameDB>().GetName(_factionEntity); }
            set
            {
                _colonyEntity.GetDataBlob<NameDB>().SetName(_factionEntity, value);
                //PropertyChanged()
            }
        }

        public ColonyScreenVM()
        {
        }

        public ColonyScreenVM(Entity colonyEntity, StaticDataStore staticData)
        {
            _colonyEntity = colonyEntity;
            _factionEntity = colonyEntity.GetDataBlob<ColonyInfoDB>().FactionEntity;
            _facilities = new ObservableCollection<FacilityVM>();
            foreach (var installation in colonyEntity.GetDataBlob<ColonyInfoDB>().Installations)
            {
                Facilities.Add(new FacilityVM(installation, _factionEntity));
            }
            _species = new Dictionary<string, long>();
            
            foreach (var kvp in colonyEntity.GetDataBlob<ColonyInfoDB>().Population)
            {
                string name = kvp.Key.GetDataBlob<NameDB>().DefaultName;

                _species.Add(name, kvp.Value);
            }

            _mineralDictionary = new Dictionary<Guid, MineralSD>();
            foreach (var mineral in staticData.Minerals)
            {
                _mineralDictionary.Add(mineral.ID, mineral);
            }
            Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Refresh(bool partialRefresh = false)
        {
            Entity planet = _colonyEntity.GetDataBlob<ColonyInfoDB>().PlanetEntity;
            var minerals = planet.GetDataBlob<SystemBodyDB>().Minerals;
            _mineralStockpile = new Dictionary<string, int>();
            _mineralDeposits = new Dictionary<string, MineralDepositInfo>();
            foreach (var kvp in minerals)
            {
                MineralSD mineral = _mineralDictionary[kvp.Key];
                MineralDeposits.Add(mineral.Name, kvp.Value);
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
