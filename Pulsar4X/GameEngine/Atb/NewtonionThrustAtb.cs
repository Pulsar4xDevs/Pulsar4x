using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.Orbital;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Interfaces;
using Pulsar4X.Components;

namespace Pulsar4X.Atb
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
                // Set the fuel type equal to the engine fuel type
                db = parentEntity.GetDataBlob<NewtonThrustAbilityDB>();
                db.FuelType = FuelType;
                // if(db.FuelType != FuelType)
                //     throw new Exception("prime entity can only have thrusters which use the same fuel type");
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

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {

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
