using System.Dynamic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class CloakAbilityDB : BaseDataBlob
    {
        /// <summary>
        /// Max ship size this cloak can handle.
        /// </summary>
        [JsonProperty]
        public int MaxShipSize { get; internal set; }

        /// <summary>
        /// TCS = ShipTonnage * CloakMultiplier
        /// </summary>
        [JsonProperty]
        public float CloakMultiplier { get; internal set; }

        public override object Clone()
        {
            return new CloakAbilityDB {MaxShipSize = MaxShipSize, CloakMultiplier = CloakMultiplier, OwningEntity = OwningEntity};
        }
    }
}
