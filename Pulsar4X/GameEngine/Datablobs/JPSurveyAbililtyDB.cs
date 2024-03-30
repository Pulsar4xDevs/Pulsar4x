using Newtonsoft.Json;

namespace Pulsar4X.Datablobs;

public class JPSurveyAbilityDB : BaseDataBlob
{
    [JsonProperty]
    public uint Speed { get; set; }
}