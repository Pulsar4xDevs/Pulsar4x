using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Pulsar4X.ViewModel
{
    public class ShipDesignVM : IViewModel
    {
        private Entity _factionEntity;

        /// <summary>
        /// Ship Design Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// this should maybe be a link to a json file or something? this is a list of names it'll automaticaly pick from when building the actual ship. 
        /// </summary>
        public string ShipNames { get; set; }

        public int Tonnage { get; set; }
        public int CrewRequired { get; set; }
        public int CrewMax { get; set; }

        /// <summary>
        /// this is a list of all components designed and availible to this empire. it should probibly include components designed but not yet researched. 
        /// these are what get generated from the DesignToEntity factory.
        /// </summary>
        public List<Entity> ComponentsDesigned { get { return _factionEntity.GetDataBlob<FactionInfoDB>().ComponentDesigns.Values.ToList(); } }
        //public List<ComponentListVM> ComponentsDesignedLists { get; set; }
        public ComponentListVM ComponentsDesignedLists { get; set; }

        /// <summary>
        /// a list of componentDesign Entities installed on teh ship, and how many of that type. 
        /// </summary>
        public DictionaryVM<Entity, int, int> ComponentsInstalled { get; set; }


        public ShipDesignVM(Entity factionEntity)
        {
            _factionEntity = factionEntity;
            ComponentsDesignedLists = new ComponentListVM(_factionEntity);
            //ComponentsDesignedLists = new List<ComponentListVM>();
            //foreach (var item in ComponentsDesigned)
            //{
            //    ComponentsDesignedLists.Add(new Compon)
            //}

        }

        public static ShipDesignVM Create(GameVM game)
        {
            return new ShipDesignVM(game.CurrentFaction);
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
            throw new NotImplementedException();
        }
    }

    public class ComponentListVM : IViewModel
    {
        public ObservableCollection<ComponentListEngineVM> Engines {get;set;}


        public ComponentListVM(Entity factionEntity)
        {
            FactionInfoDB factionInfo = factionEntity.GetDataBlob<FactionInfoDB>();
            Engines = new ObservableCollection<ComponentListEngineVM>();
            foreach (var componentDesign in factionInfo.ComponentDesigns.Values)
            {
                if (componentDesign.HasDataBlob<EnginePowerAbilityDB>())
                {
                    Engines.Add(new ComponentListEngineVM(componentDesign));
                }
            }            
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }
    }

    public class ComponentListComponentVM : IViewModel
    {
        protected Entity _componentEntity_;
        private ComponentInfoDB _designDB;

        public string Name { get; private set ; }
        public int Size { get { return _designDB.SizeInTons; } }
        public int CrewReq { get { return _designDB.CrewRequrements; } }

        public int AbilityAmount { get; protected set; }


        public ComponentListComponentVM(Entity component)
        {
            _componentEntity_ = component;
            _designDB = component.GetDataBlob<ComponentInfoDB>();

            Name = component.GetDataBlob<NameDB>().DefaultName;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }
    }

    public class ComponentListEngineVM : ComponentListComponentVM
    {
        public ComponentListEngineVM(Entity component) : base(component)
        {
            AbilityAmount = _componentEntity_.GetDataBlob<EnginePowerAbilityDB>().EnginePower;
        }
    }




}
