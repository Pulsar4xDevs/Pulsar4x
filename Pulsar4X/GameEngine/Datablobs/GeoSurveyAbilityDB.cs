using Newtonsoft.Json;

namespace Pulsar4X.Datablobs;

public class GeoSurveyAbilityDB : BaseDataBlob
{
    [JsonProperty]
    public int Speed { get; set; }
}