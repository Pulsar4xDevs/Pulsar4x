using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pulsar4X.ECSLib;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
namespace Pulsar4X.ECSLib
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

            }
        }

        private void SpecificInstances_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (object item in e.NewItems)
                {
                    KeyValuePair<Entity, PrIwObsList<ComponentInstance>> kvp = (KeyValuePair<Entity, PrIwObsList<ComponentInstance>>)item;
                    ComponentSpecificDesignVM newDesigns = new ComponentSpecificDesignVM(kvp.Key, kvp.Value);
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

        public ComponentSpecificDesignVM(Entity designEntity, PrIwObsList<ComponentInstance> specificInstances)
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
            foreach (ComponentInstance item in e.NewItems)
            {
                Instances.Add(new ComponentSpecificInstanceVM(item));
            }
        }
    }

    public class ComponentSpecificInstanceVM
    {
        
        private ComponentInstance _instance;

        public string HealthPercent { get { return (_instance?.HealthPercent() * 100).ToString() + "%" ?? " "; } }
        public string IsEnabled { get { return _instance?.IsEnabled.ToString() ?? ""; } }
        public ComponentSpecificInstanceVM(ComponentInstance instance)
        {
            _instance = instance;
        }
    }


}
