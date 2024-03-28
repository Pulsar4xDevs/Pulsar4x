using Newtonsoft.Json;

namespace Pulsar4X.Datablobs;

public class GravSurveyAbilityDB : BaseDataBlob
{
    [JsonProperty]
    public uint Speed { get; set; }
}