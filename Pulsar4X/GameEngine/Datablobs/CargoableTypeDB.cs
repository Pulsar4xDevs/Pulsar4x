using Newtonsoft.Json;
using System;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Datablobs
{
    /// <summary>
    /// Contains info on how an entitiy can be stored.
    /// NOTE an entity with this datablob must also have a MassVolumeDB
    /// </summary>
    public class CargoAbleTypeDB : BaseDataBlob , ICargoable, IComponentDesignAttribute
    {
        [JsonProperty]
        public int ID { get; private set; } = Game.GetEntityID();

        [JsonProperty]
        public string CargoTypeID { get; internal set; }

        /// <summary>
        /// NOTE! this is an entites *Design* ID, not the EntitesID.
        /// </summary>
        [JsonIgnore]
        public string UniqueID {
            get
            {
                if (OwningEntity.HasDataBlob<DesignInfoDB>())
                    return this.OwningEntity.GetDataBlob<DesignInfoDB>().DesignEntity.Id.ToString();
                else
                    return this.OwningEntity.Id.ToString();
            }
        }

        [JsonIgnore]
        public long MassPerUnit => (long)Math.Ceiling(OwningEntity.GetDataBlob<MassVolumeDB>().MassDry);

        public double VolumePerUnit => OwningEntity.GetDataBlob<MassVolumeDB>().Volume_m3;

        public double Density => OwningEntity.GetDataBlob<MassVolumeDB>().DensityDry_kgm;

        /// <summary>
        /// This should be set to true if the item has become damaged or in any other way needs to maintain state
        /// </summary>
        [JsonIgnore]
        public string Name
        {

            get
            {
                if (this.OwningEntity.GetDataBlob<NameDB>() != null)
                {
                    return this.OwningEntity.GetDataBlob<NameDB>()?.GetName(OwningEntity.FactionOwnerID);
                }
                else return "Unknown Object";
            }
        }

        public CargoAbleTypeDB()
        {
        }

        [JsonProperty]
        internal bool MustBeSpecificCargo { get; set; } = false;

        public CargoAbleTypeDB(string cargoTypeID)
        {
            CargoTypeID = cargoTypeID;
        }

        public CargoAbleTypeDB(CargoAbleTypeDB cargoTypeDB)
        {
            ID = cargoTypeDB.ID;
            CargoTypeID = cargoTypeDB.CargoTypeID;
            MustBeSpecificCargo = cargoTypeDB.MustBeSpecificCargo;
        }

        public override object Clone()
        {
            return new CargoAbleTypeDB(this);
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<CargoAbleTypeDB>())
                parentEntity.SetDataBlob(new CargoAbleTypeDB(this)); //basicaly just clone the design to the instance.
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
        }

        public string AtbName()
        {
            return "Cargoable";
        }

        public string AtbDescription()
        {
            // FIXME:
            //return "Parent can be stored in " + StaticRefLib.StaticData.CargoTypes[CargoTypeID].Name + " cargo";
            return "";
        }
    }
}