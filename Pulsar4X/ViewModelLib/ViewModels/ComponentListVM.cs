using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class ComponentDesignsListVM
    {
        private Entity _parentEntity;
        public List<ComponentSpecificDesignVM> Designs { get; } = new List<ComponentSpecificDesignVM>();

        public ComponentDesignsListVM(Entity entity)
        {
            if (entity.HasDataBlob<ComponentInstancesDB>())
                _parentEntity = entity;
            foreach (var kvp in _parentEntity.GetDataBlob<ComponentInstancesDB>().SpecificInstances)
            {
                Designs.Add(new ComponentSpecificDesignVM(kvp.Key, kvp.Value));
            }
        }
    }

    public class ComponentSpecificDesignVM : ViewModelBase
    {
        private Entity _designEntity;
        public List<ComponentSpecificInstanceVM> Instances { get; } = new List<ComponentSpecificInstanceVM>();

        private string _headerText = "";
        public string HeaderText { get { return _headerText; } set { _headerText = value; OnPropertyChanged(); } }

        public ComponentSpecificDesignVM(Entity designEntity, List<Entity> specificInstances)
        {
            _designEntity = designEntity;
            HeaderText = specificInstances.Count.ToString() + " " + designEntity.GetDataBlob<NameDB>().DefaultName;
            foreach (var item in specificInstances)
            {
                Instances.Add(new ComponentSpecificInstanceVM(item));
            }
        }
    }

    public class ComponentSpecificInstanceVM
    {
        private Entity _componentEntity;
        private ComponentInstanceInfoDB _instanceInfoDB;

        public string HealthPercent { get { return (_instanceInfoDB?.HealthPercent() * 100).ToString() + "%" ?? " "; } }
        public string IsEnabled { get { return _instanceInfoDB?.IsEnabled.ToString() ?? ""; } }
        public ComponentSpecificInstanceVM(Entity instance)
        {
            _componentEntity = instance;
            _instanceInfoDB = instance.GetDataBlob<ComponentInstanceInfoDB>();           
        }
    }


}
