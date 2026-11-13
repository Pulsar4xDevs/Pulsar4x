using System.Collections.Generic;
using System.Linq;
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

        /// <summary>The propellants a launch complex may burn. The one list every launch fuel id is checked against.</summary>
        public static readonly IReadOnlyList<string> AllowedLaunchFuels = new[] { "rp-1", "methalox", "hydrolox" };

        /// <summary>
        /// The launch fuel to use for an id from a design, a pad or a save: the id when it is on
        /// <see cref="AllowedLaunchFuels"/>, else <see cref="DefaultLaunchFuel"/>. Null, empty,
        /// whitespace, unknown ids and plant fuels such as fissile fuels all give rp-1.
        /// </summary>
        public static string NormalizeLaunchFuel(string? fuelId)
        {
            string id = fuelId?.Trim() ?? "";
            return AllowedLaunchFuels.Contains(id) ? id : DefaultLaunchFuel;
        }

        [JsonProperty]
        public double MaxTonnage { get; private set; }

        /// <summary>
        /// Cargo id of the propellant this design's pads burn for launches, picked in the
        /// component designer's "Launch Fuel" property and checked by <see cref="NormalizeLaunchFuel"/>
        /// when the design is made. Null in a save from before the choice: see <see cref="LaunchFuel"/>.
        /// </summary>
        [JsonProperty]
        public string? LaunchFuelId { get; private set; }

        /// <summary>The launch fuel, checked by <see cref="NormalizeLaunchFuel"/>.</summary>
        public string LaunchFuel => NormalizeLaunchFuel(LaunchFuelId);

        [JsonConstructor]
        private LaunchComplexAtb() { }

        public LaunchComplexAtb(double maxTonnage) : this(maxTonnage, DefaultLaunchFuel) { }

        public LaunchComplexAtb(double maxTonnage, string launchFuelId)
        {
            MaxTonnage = maxTonnage;
            LaunchFuelId = NormalizeLaunchFuel(launchFuelId);
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
