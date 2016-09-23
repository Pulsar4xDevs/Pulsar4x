using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
namespace Pulsar4X.ViewModel
{
    public class ComponentDesignsListVM : ViewModelBase
    {
        private Entity _parentEntity;
        private ComponentInstancesDB _componentInstancesDB;
        public ObservableCollection<ComponentSpecificDesignVM> Designs { get; } = new ObservableCollection<ComponentSpecificDesignVM>();

        public ComponentDesignsListVM(Entity entity)
        {
            Initialise(entity);
        }

        public void Initialise(Entity newEntity)
        {
            Designs.Clear();
            if (newEntity.HasDataBlob<ComponentInstancesDB>())
            {
                _parentEntity = newEntity;
                _componentInstancesDB = _parentEntity.GetDataBlob<ComponentInstancesDB>();
                _componentInstancesDB.SpecificInstances.CollectionChanged += SpecificInstances_CollectionChanged;
                foreach (var kvp in _componentInstancesDB.SpecificInstances)
                {
                    Designs.Add(new ComponentSpecificDesignVM(kvp.Key, kvp.Value));
                }
            }
        }

        private void SpecificInstances_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    KeyValuePair<Entity, PrIwObsList<Entity>> kvp = (KeyValuePair<Entity, PrIwObsList<Entity>>)item;
                    var newDesigns = new ComponentSpecificDesignVM(kvp.Key, kvp.Value);
                    Designs.Add(newDesigns);
                }
            }
        }
    }

    public class ComponentSpecificDesignVM : ViewModelBase
    {
        private Entity _designEntity;
        public ObservableCollection<ComponentSpecificInstanceVM> Instances { get; } = new ObservableCollection<ComponentSpecificInstanceVM>();
        public Guid EntityID { get { return _designEntity.Guid; } } 
        private string _headerText = "";
        public string HeaderText { get { return _headerText; } set { _headerText = value; OnPropertyChanged(); } }

        public ComponentSpecificDesignVM(Entity designEntity, PrIwObsList<Entity> specificInstances)
        {
            _designEntity = designEntity;
            specificInstances.CollectionChanged += SpecificInstances_CollectionChanged;
            HeaderText = specificInstances.Count.ToString() + " " + designEntity.GetDataBlob<NameDB>().DefaultName;

            foreach (var item in specificInstances)
            {
                Instances.Add(new ComponentSpecificInstanceVM(item));
            }
        }

        private void SpecificInstances_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            foreach (Entity item in e.NewItems)
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
