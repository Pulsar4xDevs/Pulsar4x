using Newtonsoft.Json;

namespace Pulsar4X.Datablobs;

public class GeoSurveyAbilityDB : BaseDataBlob
{
    [JsonProperty]
    public uint Speed { get; set; }
}