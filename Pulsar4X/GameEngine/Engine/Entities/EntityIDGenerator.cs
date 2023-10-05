using Newtonsoft.Json;

namespace Pulsar4X.Engine;

public static class EntityIDGenerator
{
    [JsonProperty]
    private static int NextId { get; set; } = 0;
    public static int GenerateUniqueID() => NextId++;
}