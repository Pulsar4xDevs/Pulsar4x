using Newtonsoft.Json;

namespace Pulsar4X.Blueprints
{
    public abstract class Blueprint
    {
        public string UniqueID { get; set; }
        
        [JsonIgnore]
        public string FullIdentifier { get; protected set; }
        [JsonIgnore]
        public string JsonFileName { get; set; }

        public void SetFullIdentifier(string modNamespace)
        {
            FullIdentifier = $"{modNamespace}:{UniqueID}";
        }
    }
}