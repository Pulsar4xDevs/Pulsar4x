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
        private ObservableCollection<FacilityVM> _facilities;
        public ObservableCollection<FacilityVM> Facilities
        {
            get { return _facilities; }
        }

        private Dictionary<string, long> _species;
        public Dictionary<string, long> Species { get { return _species; } }


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

        public ColonyScreenVM(Entity colonyEntity)
        {
            _colonyEntity = colonyEntity;
            _factionEntity = colonyEntity.GetDataBlob<ColonyInfoDB>().FactionEntity;
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
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }
    }


    public class FacilityVM
    {
        private Entity _facilityEntity;
        private Entity _factionEntity;

        public string Name { get { return _facilityEntity.GetDataBlob<NameDB>().GetName(_factionEntity); } }

        public int WorkersRequired { get { return _facilityEntity.GetDataBlob<ComponentInfoDB>().CrewRequrements; } }

        public FacilityVM()
        {
        }

        public FacilityVM(Entity facilityEntity, Entity factionEntity)
        {
            _facilityEntity = facilityEntity;
            _facilityEntity = factionEntity;
        }
    }
}
