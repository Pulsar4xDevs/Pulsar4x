namespace Pulsar4X.Modding
{
    public abstract class SerializableGameData
    {
        public string UniqueID { get; set; }
        public string FullIdentifier { get; private set; }

        public void SetFullIdentifier(string modNamespace)
        {
            FullIdentifier = $"{modNamespace}:{UniqueID}";
        }
    }
}