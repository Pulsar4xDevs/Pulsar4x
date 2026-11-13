using Newtonsoft.Json;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;

namespace Pulsar4X.Ships
{
    public class LaunchComplexAtb : IComponentDesignAttribute
    {
        /// <summary>
        /// The launch fuel of a design or pad that has none: a save from before the design chose
        /// one, or a pad made without a design. Matches the template's default.
        /// </summary>
        public const string DefaultLaunchFuel = "rp-1";

        [JsonProperty]
        public double MaxTonnage { get; private set; }

        /// <summary>
        /// Cargo id of the propellant this design's pads burn for launches, picked in the
        /// component designer. Null in a save from before the choice: see <see cref="LaunchFuel"/>.
        /// </summary>
        [JsonProperty]
        public string? LaunchFuelId { get; private set; }

        /// <summary>The launch fuel, falling back to <see cref="DefaultLaunchFuel"/>.</summary>
        public string LaunchFuel => string.IsNullOrEmpty(LaunchFuelId) ? DefaultLaunchFuel : LaunchFuelId;

        [JsonConstructor]
        private LaunchComplexAtb() { }

        public LaunchComplexAtb(double maxTonnage) : this(maxTonnage, DefaultLaunchFuel) { }

        public LaunchComplexAtb(double maxTonnage, string launchFuelId)
        {
            MaxTonnage = maxTonnage;
            LaunchFuelId = launchFuelId;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            var pad = new LaunchPad
            {
                MaxTonnage = (long)MaxTonnage,
                LaunchFuelId = LaunchFuel,
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
            return $"Launch pad capable of launching ships up to {MaxTonnage:N0} kg, burning {LaunchFuel}";
        }
    }
}
