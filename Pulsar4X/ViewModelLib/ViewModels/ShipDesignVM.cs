using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ViewModel.ViewModels
{
    class ShipDesignVM : IViewModel
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

        /// <summary>
        /// a list of componentDesign Entities installed on teh ship, and how many of that type. 
        /// </summary>
        public DictionaryVM<Entity, int> ComponentsInstalled { get; set; }


        public ShipDesignVM(Entity factionEntity)
        {
            _factionEntity = factionEntity;

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

    class ComponentListVM : IViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }
    }

    class ComponentListComponentVM : IViewModel
    {
        private Entity _componentEntity;
        private ComponentInfoDB _designDB;

        public string Name { get; private set ; }
        public int Size { get { return _designDB.SizeInTons; } }
        public int AbilityAmount { get; set; }


        public ComponentListComponentVM(Entity component)
        {
            _componentEntity = component;
            _designDB = component.GetDataBlob<ComponentInfoDB>();

            Name = component.GetDataBlob<NameDB>().DefaultName;
            
           
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }
    }
}
