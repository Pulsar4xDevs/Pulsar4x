using System;
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
        public double FuelUsage;

        public NewtonionThrustAtb(double exhaustVelocity, Guid fuelType, double fuelUsage)
        {
            //ThrustInNewtons = thrust;
            ExhaustVelocity = exhaustVelocity;
            FuelType = fuelType;
            FuelUsage = fuelUsage;
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
            db.FuelUsage += FuelUsage;
            db.ThrustInNewtons += ExhaustVelocity * FuelUsage;
        }
    }

    public class NewtonThrustAbilityDB : BaseDataBlob
    {
        
        public double ThrustInNewtons = 0;
        //public double SpecificImpulseASL = 0;
        public double ExhaustVelocity = 0;
        public Guid FuelType; //todo: change this to a list and enable multple fuel types. 
        public double FuelUsage = 0;

        [JsonConstructor]
        private NewtonThrustAbilityDB()
        {
        }

        public NewtonThrustAbilityDB(Guid fuelType)
        {
            FuelType = fuelType;
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }

    public class NewtonMoveDB : BaseDataBlob
    {
        internal DateTime LastProcessDateTime = new DateTime();
        
        public Vector3 DeltaVToExpend_AU  { get; internal set; }
        public DateTime ActionOnDateTime { get; internal set; }
        
        


        public Vector3 CurrentVector_ms { get; internal set; }

        public Entity SOIParent { get; internal set; }
        public double ParentMass { get; internal set; }

        [JsonConstructor]
        private NewtonMoveDB() { }

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
