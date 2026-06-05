using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Ships
{
    public class LaunchComplexAtb : IComponentDesignAttribute
    {
        [JsonProperty]
        public double MaxTonnage { get; private set; }

        [JsonConstructor]
        private LaunchComplexAtb() { }

        public LaunchComplexAtb(double maxTonnage)
        {
            MaxTonnage = maxTonnage;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            var pad = new LaunchPad
            {
                MaxTonnage = (long)MaxTonnage
            };

            if (!parentEntity.TryGetDataBlob<LaunchComplexDB>(out var db))
            {
                db = new LaunchComplexDB(componentInstance.UniqueID, pad);
                parentEntity.SetDataBlob(db);
            }
            else
            {
                db.Pads[componentInstance.UniqueID] = pad;
            }
        }

        public void OnComponentUninstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (parentEntity.TryGetDataBlob<LaunchComplexDB>(out var db))
            {
                db.Pads.Remove(componentInstance.UniqueID);
                if (db.Pads.Count == 0)
                {
                    parentEntity.RemoveDataBlob<LaunchComplexDB>();
                }
            }
        }

        public string AtbName()
        {
            return "Launch Complex";
        }

        public string AtbDescription()
        {
            return $"Launch pad capable of launching ships up to {MaxTonnage:N0} kg";
        }
    }
}
