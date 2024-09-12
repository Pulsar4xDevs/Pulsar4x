using Newtonsoft.Json;

namespace Pulsar4X.Datablobs;

public class MoveTowardsTargetDB : BaseDataBlob
{
    [JsonProperty]
    public int TargetId { get; set; }

    [JsonProperty]
    public double DistanceOffset { get; set; } = 100; // 100 meters is probably close enough for a default

    [JsonProperty]
    public double Speed { get; set; }
}