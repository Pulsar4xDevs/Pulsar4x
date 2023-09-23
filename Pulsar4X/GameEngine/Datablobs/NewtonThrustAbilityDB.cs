using System;
using Newtonsoft.Json;
using Pulsar4X.Orbital;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Components;

namespace Pulsar4X.Datablobs
{
    public class NewtonThrustAbilityDB : BaseDataBlob, IAbilityDescription
    {

        public double ThrustInNewtons = 0;
        //public double SpecificImpulseASL = 0;
        public double ExhaustVelocity = 0;
        public string FuelType; //todo: change this to a list and enable multple fuel types.

        /// <summary>
        /// in Kg/s
        /// </summary>
        public double FuelBurnRate = 0;
        public double TotalFuel_kg { get; private set; }

        /// <summary>
        /// non fuel mass. will need updating when non fuel cargo is added/removed.
        /// (should be the mass of the ship plus any cargo including fuel not usable by this ship).
        /// </summary>
        //public double DryMass_kg { get; internal set; }

        /// <summary>
        /// Amount of DeltaV the ship has availible.
        /// </summary>
        public double DeltaV { get; private set; } = 0;

        /// <summary>
        /// removes fuel and correct amount of DV.
        /// </summary>
        /// <param name="fuel">in kg</param>
        internal void BurnFuel(double fuel, double wetMass_kg)
        {
            TotalFuel_kg -= fuel;
            double dryMass = wetMass_kg - fuel;
            DeltaV = OrbitMath.TsiolkovskyRocketEquation(wetMass_kg, dryMass, ExhaustVelocity);
        }

        /// <summary>
        /// removes deltaV and correct amount of fuel.
        /// does NOT update the ships total mass or remove fuel from VolumeStorageDB
        /// </summary>
        /// <param name="dv"></param>
        /// <returns>fuel Burned in kg</returns>
        internal double BurnDeltaV(double dv, double WetMass_kg)
        {
            DeltaV -= dv;
            double fuelBurned = OrbitMath.TsiolkovskyFuelUse(WetMass_kg, ExhaustVelocity, dv);
            TotalFuel_kg -= fuelBurned;
            return fuelBurned;
        }

        /// <summary>
        /// Adds fuel, and updates DeltaV.
        /// </summary>
        /// <param name="fuel"></param>
        internal void AddFuel(double fuel, double wetMass_kg)
        {
            TotalFuel_kg += fuel;
            double dryMass = wetMass_kg - fuel;
            DeltaV = OrbitMath.TsiolkovskyRocketEquation(wetMass_kg, dryMass, ExhaustVelocity);
        }

        /// <summary>
        /// Sets a given amount of fuel, and updates DeltaV.
        /// </summary>
        /// <param name="fuel"></param>
        internal void SetFuel(double fuel, double wetMass_kg)
        {
            TotalFuel_kg = fuel;
            double dryMass = wetMass_kg - fuel;
            DeltaV = OrbitMath.TsiolkovskyRocketEquation(wetMass_kg, dryMass, ExhaustVelocity);
        }

        [JsonConstructor]
        private NewtonThrustAbilityDB()
        {
        }

        public NewtonThrustAbilityDB(string fuelType)
        {
            FuelType = fuelType;
        }

        public NewtonThrustAbilityDB(NewtonThrustAbilityDB db)
        {
            ThrustInNewtons = db.ThrustInNewtons;
            ExhaustVelocity = db.ExhaustVelocity;
            FuelType = db.FuelType;
            FuelBurnRate = db.FuelBurnRate;
            //DryMass_kg = db.DryMass_kg;
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
        public string FuelType;

        /// <summary>
        /// in kg/s (mass)
        /// </summary>
        public double FuelBurnRate;

        public NewtonionThrustAtb(double exhaustVelocity, string fuelType, double fuelBurnRate)
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
}