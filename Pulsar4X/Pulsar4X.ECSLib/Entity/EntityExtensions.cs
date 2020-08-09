namespace Pulsar4X.ECSLib
{
    public static class EntityExtensions
    {
        public static string GetDefaultName(this Entity entity)
        {
            if (entity.HasDataBlob<NameDB>())
                return entity.GetDataBlob<NameDB>().DefaultName;
            return "Unknown";
        }
    }
}
