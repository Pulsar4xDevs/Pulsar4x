using System;
using System.Collections.Generic;
using System.ComponentModel;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel
{
    public class FactionVM
    {
        private Entity _entity;
        
        /// <summary>
        /// The default name of the faction, i.e. the name it knows itself by.
        /// </summary>
        public string Name { get { return _entity.GetDataBlob<NameDB>().DefaultName; } }

        public List<SpeciesVM> Species;

        public FactionVM()
        {
        }

        private FactionVM(Entity factionEntity)
        {
            _entity = factionEntity;
            Species = new List<SpeciesVM>();
            foreach (var speciesEntity in factionEntity.GetDataBlob<FactionInfoDB>().Species)
            {
                Species.Add(SpeciesVM.Create(speciesEntity));
            }
        }

        public static FactionVM Create(Entity factionEntity)
        {
            if (!factionEntity.HasDataBlob<FactionInfoDB>())
                throw new Exception("Entity is not a faction or does not have a FactionInfoDB");
            return new FactionVM(factionEntity);
        }

        #region IViewModel

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion 
    }
}