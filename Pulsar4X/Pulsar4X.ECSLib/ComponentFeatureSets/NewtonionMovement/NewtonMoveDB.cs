using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{
    public class NewtonionThrustAtb : IComponentDesignAttribute
    {

        //public double SpecificImpulseASL; //maybe future do stuff with planet to space efficencies.
        
        /// <summary>
        /// in m/s
        /// </summary>
        public double ExhaustVelocity;
        public Guid FuelType;
        
        /// <summary>
        /// in kg/s (mass)
        /// </summary>
        public double FuelBurnRate;

        public NewtonionThrustAtb(double exhaustVelocity, Guid fuelType, double fuelBurnRate)
        {
            //ThrustInNewtons = thrust;
            ExhaustVelocity = exhaustVelocity;
            FuelType = fuelType;
            FuelBurnRate = fuelBurnRate;
        }
        

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            NewtonThrustAbilityDB db;
            if (!parentEntity.HasDataBlob<NewtonThrustAbilityDB>())
            {
                db = new NewtonThrustAbilityDB(FuelType);
                parentEntity.SetDataBlob(db);
            }
            else
            {
                db = parentEntity.GetDataBlob<NewtonThrustAbilityDB>();
                if(db.FuelType != FuelType)
                    throw new Exception("prime entity can only have thrusters which use the same fuel type");
                //todo: fix so we can use different fuel types on the prime entity.
            }

            //db.ThrustInNewtons += ThrustInNewtons;
            db.ExhaustVelocity = ExhaustVelocity;
            db.FuelBurnRate += FuelBurnRate;
            db.ThrustInNewtons += ExhaustVelocity * FuelBurnRate;
            
            /*
            var wetmass = parentEntity.GetDataBlob<MassVolumeDB>().Mass;
            ProcessedMaterialSD foo = StaticRefLib.StaticData.CargoGoods.GetMaterials()[FuelType];
            var cargo = parentEntity.GetDataBlob<CargoStorageDB>();
            var fuelAmount = StorageSpaceProcessor.GetAmount(cargo, foo);
            var dryMass = wetmass - fuelAmount;
            db.DeltaV = OrbitMath.TsiolkovskyRocketEquation(wetmass, dryMass, ExhaustVelocity);
            */
        }
    }

    public class NewtonThrustAbilityDB : BaseDataBlob
    {
        
        public double ThrustInNewtons = 0;
        //public double SpecificImpulseASL = 0;
        public double ExhaustVelocity = 0;
        public Guid FuelType; //todo: change this to a list and enable multple fuel types. 
        
        /// <summary>
        /// in Kg/s
        /// </summary>
        public double FuelBurnRate = 0;

        public double DeltaV = 0;
        
        [JsonConstructor]
        private NewtonThrustAbilityDB()
        {
        }

        public NewtonThrustAbilityDB(Guid fuelType)
        {
            FuelType = fuelType;
        }

        public NewtonThrustAbilityDB(NewtonThrustAbilityDB db)
        {
            ThrustInNewtons = db.ThrustInNewtons;
            ExhaustVelocity = db.ExhaustVelocity;
            FuelType = db.FuelType;
            FuelBurnRate = db.FuelBurnRate;
            DeltaV = db.DeltaV;
        }

        public override object Clone()
        {
            return new NewtonThrustAbilityDB(this);
        }
    }

    public class NewtonMoveDB : BaseDataBlob
    {
        internal DateTime LastProcessDateTime = new DateTime();
        
        /// <summary>
        /// Orbital Frame Of Reference: Y is prograde
        /// </summary>
        public Vector3 DeltaVForManuver_FoRO_m { get; internal set; }
        /// <summary>
        /// Orbital Frame Of Reference: Y is prograde
        /// </summary>
        public Vector3 DeltaVForManuver_FoRO_AU
        {
            get { return Distance.MToAU(DeltaVForManuver_FoRO_m); }
        }
        public DateTime ActionOnDateTime { get; internal set; }
        
        public Queue<(Vector3 deltaV, DateTime nextManuverTime)> Manuvers = new Queue<(Vector3, DateTime)>();

        /// <summary>
        /// Parent ralitive velocity vector. 
        /// </summary>
        public Vector3 CurrentVector_ms { get; internal set; }

        public Entity SOIParent { get; internal set; }
        public double ParentMass { get; internal set; }

        [JsonConstructor]
        private NewtonMoveDB() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sphereOfInfluenceParent"></param>
        /// <param name="velocity_ms">ParentRalitive Velocity</param>
        public NewtonMoveDB(Entity sphereOfInfluenceParent, Vector3 velocity_ms)
        {
            CurrentVector_ms = velocity_ms;
            SOIParent = sphereOfInfluenceParent;
            ParentMass = SOIParent.GetDataBlob<MassVolumeDB>().Mass;
            LastProcessDateTime = sphereOfInfluenceParent.Manager.ManagerSubpulses.StarSysDateTime;
        }
        public NewtonMoveDB(NewtonMoveDB db)
        {
            LastProcessDateTime = db.LastProcessDateTime;
            CurrentVector_ms = db.CurrentVector_ms;
            SOIParent = db.SOIParent;
            ParentMass = db.ParentMass; 
        }
        public override object Clone()
        {
            return new NewtonMoveDB(this);
        }
    }
}
