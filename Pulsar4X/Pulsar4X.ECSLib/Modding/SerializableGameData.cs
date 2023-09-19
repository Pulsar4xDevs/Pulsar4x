namespace Pulsar4X.Modding
{
    public abstract class SerializableGameData
    {
        public string UniqueId { get; set; }
        public string FullIdentifier { get; private set; }

        public void SetFullIdentifier(string modNamespace)
        {
            FullIdentifier = $"{modNamespace}:{UniqueId}";
        }
    }
}