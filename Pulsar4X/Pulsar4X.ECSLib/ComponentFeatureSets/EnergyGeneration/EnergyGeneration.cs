using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib.ComponentFeatureSets.CargoStorage;

namespace Pulsar4X.ECSLib
{
    public class EnergyGenerationAtb : IComponentDesignAttribute
    {
        public Guid FuelType; //min or mat.
        
        public double FuelUsedAtMax;  //KgPerS
        
        public Guid EnergyTypeID;
        
        public double PowerOutputMax; //Mw

        public double Lifetime;
        
        public EnergyGenerationAtb(Guid fueltype, double fuelUsedAtMax, Guid energyTypeID, double powerOutputMax, double lifetime)
        {
            FuelType = fueltype;
            PowerOutputMax = powerOutputMax;
            FuelUsedAtMax = fuelUsedAtMax;
            EnergyTypeID = energyTypeID;
            Lifetime = lifetime;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            Guid resourceID = EnergyTypeID;
            ICargoable energyCargoable = StaticRefLib.StaticData.GetICargoable(resourceID);
            EntityEnergyGenAbilityDB entityGenDB;
            if (!parentEntity.HasDataBlob<EntityEnergyGenAbilityDB>())
            {
                entityGenDB = new EntityEnergyGenAbilityDB();
                entityGenDB.EnergyType = energyCargoable;
                parentEntity.SetDataBlob(entityGenDB);
            }
            else
            {
                entityGenDB = parentEntity.GetDataBlob<EntityEnergyGenAbilityDB>();
                

                
                if(entityGenDB.EnergyType != energyCargoable)//this is just to reduce complexity. we can add this ability later.
                    throw new Exception("PrimeEntity cannot use two different energy types");
            }

            entityGenDB.TotalOutputMax += PowerOutputMax;

            if(entityGenDB.TotalFuelUseAtMax.type != FuelType)
                throw new Exception("PrimeEntity cannot have power plants that use different fuel types");
            double maxUse = entityGenDB.TotalFuelUseAtMax.maxUse + FuelUsedAtMax;
            entityGenDB.TotalFuelUseAtMax = (FuelType, maxUse);
            entityGenDB.LocalFuel = maxUse * Lifetime;
            
            //add enough energy store for 1s of running. 
            if (entityGenDB.EnergyStore.ContainsKey(EnergyTypeID))
            {
                var foo = entityGenDB.EnergyStore[EnergyTypeID];
                entityGenDB.EnergyStore[EnergyTypeID] = (foo.stored, foo.maxStore + PowerOutputMax);
            }
            else
            {
                entityGenDB.EnergyStore[EnergyTypeID] = (0, PowerOutputMax);
            }

        }
    }

    public class EnergyStoreAtb : IComponentDesignAttribute
    {
        //<type, amount>
        public Guid EnergyType;
        public double MaxStore;

        public EnergyStoreAtb(Guid energyType, double maxStore)
        {
            EnergyType = energyType;
            MaxStore = maxStore;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            EntityEnergyGenAbilityDB entityGenDB;
            if (!parentEntity.HasDataBlob<EntityEnergyGenAbilityDB>())
            {
                entityGenDB = new EntityEnergyGenAbilityDB();
                parentEntity.SetDataBlob(entityGenDB);
            }
            else
            {
                entityGenDB = parentEntity.GetDataBlob<EntityEnergyGenAbilityDB>();
            }
            if (entityGenDB.EnergyStore.ContainsKey(EnergyType))
            {
                var foo = entityGenDB.EnergyStore[EnergyType];
                entityGenDB.EnergyStore[EnergyType] = (foo.stored, foo.maxStore + MaxStore);
            }
            else
            {
                entityGenDB.EnergyStore[EnergyType] = (0, MaxStore);
            }
        }
    }

    public class EntityEnergyGenAbilityDB : BaseDataBlob
    {
        internal DateTime dateTimeLastProcess;
        public ICargoable EnergyType;
        public double TotalOutputMax = 0;

        public (Guid type, double maxUse) TotalFuelUseAtMax;

        public double PowerReq;
        public double Load;
        
        public Dictionary<Guid, (double stored, double maxStore)> EnergyStore = new Dictionary<Guid, (double stored, double maxStore)>();
        public double LocalFuel;
        
        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class EnergyGenProcessor
    {
        
        public static void EnergyGen(Entity entity, DateTime atDateTime)
        {
            EntityEnergyGenAbilityDB _energyGenDB = entity.GetDataBlob<EntityEnergyGenAbilityDB>();

            TimeSpan t = atDateTime - _energyGenDB.dateTimeLastProcess; 
            
            Guid energyType = _energyGenDB.EnergyType.ID;
            var store = _energyGenDB.EnergyStore[energyType];
            
            var extraEnergy = _energyGenDB.TotalOutputMax - _energyGenDB.PowerReq;

            extraEnergy = store.stored + extraEnergy;
            double overflow = store.maxStore - extraEnergy;
            
            _energyGenDB.EnergyStore[energyType] = (extraEnergy - overflow, store.maxStore);

            double use = _energyGenDB.TotalOutputMax - overflow;

            double load = _energyGenDB.TotalOutputMax / use;
            _energyGenDB.Load = load;

            double fueluse = _energyGenDB.TotalFuelUseAtMax.maxUse * load;
            _energyGenDB.LocalFuel -= fueluse * t.TotalSeconds;
            
            _energyGenDB.dateTimeLastProcess = atDateTime;

        }


    }
}