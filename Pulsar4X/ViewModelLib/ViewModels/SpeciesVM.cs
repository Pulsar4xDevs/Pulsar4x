using System;
using System.ComponentModel;
using Pulsar4X.ECSLib;



namespace Pulsar4X.ViewModel
{
    public class SpeciesVM : IViewModel
    {
        private Entity _entity;
        private SpeciesDB _speciesDB { get { return _entity.GetDataBlob<SpeciesDB>(); } }

        #region Properties

        //default name;
        public string Name { get { return _entity.GetDataBlob<NameDB>().DefaultName; } }

        public double BaseGravity
        {
            get { return _speciesDB.BaseGravity; }
        }

        public double MinimumGravityConstraint
        {
            get { return _speciesDB.MinimumGravityConstraint; }
        }


        public double MaximumGravityConstraint
        {
            get { return _speciesDB.MaximumGravityConstraint; }
        }

        public double BasePressure
        {
            get { return _speciesDB.BasePressure; }
        }


        public double MinimumPressureConstraint
        {
            get { return _speciesDB.MinimumPressureConstraint; }
        }


        public double MaximumPressureConstraint
        {
            get { return _speciesDB.MaximumPressureConstraint; }
        }


        public double BaseTemperature
        {
            get { return _speciesDB.BaseTemperature; }
        }

        public double MinimumTemperatureConstraint
        {
            get { return _speciesDB.BasePressure; }
        }


        public double MaximumTemperatureConstraint
        {
            get { return _speciesDB.MaximumTemperatureConstraint; }
        }

        #endregion


        #region Constructors
        //arg-less constructor for crosslanguage support, I think this is needed... if we ever go there.
        public SpeciesVM()
        {
        }

        private SpeciesVM(Entity speciesEntity)
        {
            _entity = speciesEntity;
        }

        //use this to construct.
        public static SpeciesVM Create(Entity speciesEntity)
        {
            if(!speciesEntity.HasDataBlob<SpeciesDB>())
                throw new Exception("Entity not a Species Entity or does not contain a SpeciesDB");
            return new SpeciesVM(speciesEntity);
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        public void Refresh(bool partialRefresh = false)
        {
            throw new NotImplementedException();
        }
    }
}
