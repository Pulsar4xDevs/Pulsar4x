using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Orbital;

namespace Pulsar4X.ECSLib
{
    public class NewtonionThrustAtb : IComponentDesignAttribute
    {

        //public double SpecificImpulseASL; //maybe future do stuff with planet to space efficencies.
        
        /// <summary>
        /// in m/s
        /// </summary>
        public double ExhaustVelocity;
        /// <summary>
        /// this is a specific mineral/refined materal etc, rather than a cargo type
        /// </summary>
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
        
        public string AtbName()
        {
            return "Newton Thrust";
        }

        public string AtbDescription()
        {

            return " ";
        }
    }

    public class NewtonThrustAbilityDB : BaseDataBlob, IAbilityDescription
    {
        
        public double ThrustInNewtons = 0;
        //public double SpecificImpulseASL = 0;
        public double ExhaustVelocity = 0;
        public Guid FuelType; //todo: change this to a list and enable multple fuel types. 
        
        /// <summary>
        /// in Kg/s
        /// </summary>
        public double FuelBurnRate = 0;
        public double TotalFuel_kg { get; private set; }
        
        /// <summary>
        /// non fuel mass. will need updating when non fuel cargo is added/removed.
        /// (should be the mass of the ship plus any cargo including fuel not usable by this ship).
        /// </summary>
        public double DryMass_kg { get; internal set; } 
        public double DeltaV { get; private set; } = 0;

        /// <summary>
        /// removes fuel and correct amount of DV.
        /// </summary>
        /// <param name="fuel">in kg</param>
        internal void BurnFuel(double fuel)
        {
            TotalFuel_kg -= fuel;
            DeltaV = OrbitMath.TsiolkovskyRocketEquation(TotalFuel_kg, DryMass_kg, ExhaustVelocity);
        }

        /// <summary>
        /// removes deltaV and correct amount of fuel.
        /// </summary>
        /// <param name="dv"></param>
        internal void BurnDeltaV(double dv)
        {
            DeltaV -= dv;
            TotalFuel_kg -= OrbitMath.TsiolkovskyFuelUse(DryMass_kg + TotalFuel_kg, ExhaustVelocity, dv);
        }

        /// <summary>
        /// Adds fuel, and updates DeltaV.
        /// </summary>
        /// <param name="fuel"></param>
        internal void AddFuel(double fuel)
        {
            TotalFuel_kg += fuel;
            DeltaV = OrbitMath.TsiolkovskyRocketEquation(DryMass_kg + TotalFuel_kg, DryMass_kg, ExhaustVelocity);
        }
        
        /// <summary>
        /// Sets a given amount of fuel, and updates DeltaV.
        /// </summary>
        /// <param name="fuel"></param>
        internal void SetFuel(double fuel)
        {
            TotalFuel_kg = fuel;
            DeltaV = OrbitMath.TsiolkovskyRocketEquation(DryMass_kg + TotalFuel_kg, DryMass_kg, ExhaustVelocity);
        }

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
            DryMass_kg = db.DryMass_kg;
            TotalFuel_kg = db.TotalFuel_kg;
            DeltaV = db.DeltaV;
        }

        public override object Clone()
        {
            return new NewtonThrustAbilityDB(this);
        }

        public string AbilityName()
        {
            return "Newtonion Thrust";
        }

        public string AbilityDescription()
        {
            string desc = "";
            desc += "Thrust : " + ThrustInNewtons + " N\n";
            desc += "Δv : " + DeltaV + " m/s\n";
            return desc;
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
        
        /// <summary>
        /// Parent relative velocity vector. 
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
        /// <param name="velocity_ms">Parentrelative Velocity</param>
        public NewtonMoveDB(Entity sphereOfInfluenceParent, Vector3 velocity_ms)
        {
            CurrentVector_ms = velocity_ms;
            SOIParent = sphereOfInfluenceParent;
            ParentMass = SOIParent.GetDataBlob<MassVolumeDB>().MassDry;
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
        
        public KeplerElements GetElements()
        {
            // if there is not a change in Dv then the kepler elements wont have changed, it might be better to store them?
            double myMass = OwningEntity.GetDataBlob<MassVolumeDB>().MassDry;
            var sgp = OrbitMath.CalculateStandardGravityParameterInM3S2(myMass, ParentMass);
            var pos = OwningEntity.GetDataBlob<PositionDB>().RelativePosition_m;
            var dateTime = OwningEntity.StarSysDateTime;
            var ke = OrbitMath.KeplerFromPositionAndVelocity(sgp, pos, CurrentVector_ms, dateTime);
            return ke;
        }
    }
}
