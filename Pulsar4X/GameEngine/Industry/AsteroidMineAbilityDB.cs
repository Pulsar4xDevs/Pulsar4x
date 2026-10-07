using Newtonsoft.Json;
using Pulsar4X.Datablobs;

namespace Pulsar4X.Industry;

/// <summary>
/// Ship can mine an asteroid it is orbiting. This is not <see cref="MiningDB"/>.
/// Colony mines stay on the daily processor.
/// </summary>
public class AsteroidMineAbilityDB : BaseDataBlob
{
    [JsonProperty]
    public int UnitsPerDay { get; set; }
}
