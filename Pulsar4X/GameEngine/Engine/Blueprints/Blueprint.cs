using Newtonsoft.Json;

namespace Pulsar4X.Blueprints
{
    public abstract class Blueprint
    {
        public string UniqueID { get; set; }
        public string FullIdentifier { get; protected set; }
        public string JsonFileName { get; set; }

        public void SetFullIdentifier(string modNamespace)
        {
            FullIdentifier = $"{modNamespace}:{UniqueID}";
        }
    }
}